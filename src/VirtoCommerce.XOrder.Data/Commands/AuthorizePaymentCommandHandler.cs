using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Model.Requests;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.XOrder.Core.Commands;
using VirtoCommerce.XOrder.Core.Models;
using VirtoCommerce.XOrder.Core.Validators;

namespace VirtoCommerce.XOrder.Data.Commands
{
    public class AuthorizePaymentCommandHandler : PaymentCommandHandlerBase, IRequestHandler<AuthorizePaymentCommand, AuthorizePaymentResult>
    {
        private readonly ICustomerOrderService _customerOrderService;
        private readonly IStoredPaymentMethodManager _storedPaymentMethodManager;
        private readonly ILogger<AuthorizePaymentCommandHandler> _logger;

        public AuthorizePaymentCommandHandler(
            ICustomerOrderService customerOrderService,
            IPaymentService paymentService,
            IStoreService storeService,
            IStoredPaymentMethodManager storedPaymentMethodManager,
            ILogger<AuthorizePaymentCommandHandler> logger)
            : base(customerOrderService, paymentService, storeService)
        {
            _customerOrderService = customerOrderService;
            _storedPaymentMethodManager = storedPaymentMethodManager;
            _logger = logger;
        }

        public async Task<AuthorizePaymentResult> Handle(AuthorizePaymentCommand request, CancellationToken cancellationToken)
        {
            var paymentInfo = await GetPaymentInfo(request);

            var validationResult = AbstractTypeFactory<PaymentRequestValidator>.TryCreateInstance().Validate(paymentInfo);
            if (!validationResult.IsValid)
            {
                return ErrorResult<AuthorizePaymentResult>(validationResult.Errors.FirstOrDefault()?.ErrorMessage);
            }

            var parameters = GetParameters(request);

            var validatePostProcessResult = await paymentInfo.Payment.PaymentMethod.ValidatePostProcessRequestAsync(parameters, cancellationToken);
            if (!validatePostProcessResult.IsSuccess)
            {
                return ErrorResult<AuthorizePaymentResult>(validatePostProcessResult.ErrorMessage);
            }

            var postProcessPaymentRequest = new PostProcessPaymentRequest
            {
                OrderId = paymentInfo.CustomerOrder.Id,
                Order = paymentInfo.CustomerOrder,
                PaymentId = paymentInfo.Payment.Id,
                Payment = paymentInfo.Payment,
                StoreId = paymentInfo.CustomerOrder.StoreId,
                Store = paymentInfo.Store,
                CultureName = !string.IsNullOrEmpty(request.CultureName)
                    ? request.CultureName
                    : paymentInfo.Store?.DefaultLanguage,
                Parameters = parameters,
            };

            // Save the card or pay with a saved one: the posted storeForFutureUse, consentId and storedPaymentMethodId
            // become typed request fields, and a saved card is checked against the order's customer.
            var storedPaymentContext = GetStoredPaymentContext(paymentInfo.CustomerOrder, paymentInfo.Payment);
            var prepareResult = await _storedPaymentMethodManager.PrepareAsync(postProcessPaymentRequest, storedPaymentContext);
            if (!prepareResult.IsSuccess)
            {
                return ErrorResult<AuthorizePaymentResult>(prepareResult.ErrorMessage);
            }

            var processPaymentRequestResult = await paymentInfo.Payment.PaymentMethod.PostProcessPaymentAsync(postProcessPaymentRequest, cancellationToken);

            if (processPaymentRequestResult.IsSuccess)
            {
                paymentInfo.Payment.Status = processPaymentRequestResult.NewPaymentStatus.ToString();

                await _customerOrderService.SaveChangesAsync(new[] { paymentInfo.CustomerOrder });

                await SaveStoredPaymentMethodAsync(postProcessPaymentRequest, processPaymentRequestResult, storedPaymentContext);
            }

            var result = new AuthorizePaymentResult
            {
                IsSuccess = processPaymentRequestResult.IsSuccess,
                ErrorMessage = processPaymentRequestResult.ErrorMessage,
            };

            return result;
        }

        protected virtual StoredPaymentContext GetStoredPaymentContext(CustomerOrder order, PaymentIn payment)
        {
            return new StoredPaymentContext
            {
                StoreId = order.StoreId,
                CustomerId = order.CustomerId,
                OrganizationId = order.OrganizationId,
                PaymentMethodCode = payment.GatewayCode,
                OrderId = order.Id,
                PaymentId = payment.Id,
            };
        }

        /// <summary>
        /// Records the card the payment method saved, or marks the saved card that paid as used.
        /// The payment is already approved and saved, so a failure here is logged instead of failing it.
        /// </summary>
        protected virtual async Task SaveStoredPaymentMethodAsync(PostProcessPaymentRequest request, PostProcessPaymentRequestResult result, StoredPaymentContext context)
        {
            try
            {
                await _storedPaymentMethodManager.SaveFromPaymentAsync(request, result, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Saving the card of payment {PaymentId} of order {OrderId} failed", context.PaymentId, context.OrderId);
            }
        }
    }
}
