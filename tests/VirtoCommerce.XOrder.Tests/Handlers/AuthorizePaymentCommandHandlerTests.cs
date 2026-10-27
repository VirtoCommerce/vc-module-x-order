using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Model.Requests;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.XOrder.Core.Commands;
using VirtoCommerce.XOrder.Data.Commands;
using Xunit;

namespace VirtoCommerce.XOrder.Tests.Handlers
{
    public class AuthorizePaymentCommandHandlerTests
    {
        private readonly RecordingPaymentMethod _paymentMethod = new() { IsActive = true };
        private readonly Mock<IStoredPaymentMethodManager> _manager = new();
        private readonly Mock<ICustomerOrderService> _orderService = new();
        private readonly CustomerOrder _order;

        public AuthorizePaymentCommandHandlerTests()
        {
            var payment = new PaymentIn { Id = "payment-1", GatewayCode = RecordingPaymentMethod.MethodCode, PaymentMethod = _paymentMethod, PaymentStatus = PaymentStatus.Pending };
            _order = new CustomerOrder { Id = "order-1", StoreId = "store-1", CustomerId = "customer-1", OrganizationId = "organization-1", InPayments = [payment] };

            _orderService
                .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(() => [_order]);
            _manager
                .Setup(x => x.PrepareAsync(It.IsAny<PostProcessPaymentRequest>(), It.IsAny<StoredPaymentContext>()))
                .ReturnsAsync(StoredPaymentMethodOperationResult.Success());
        }

        private AuthorizePaymentCommandHandler Handler
        {
            get
            {
                var storeService = new Mock<IStoreService>();
                storeService
                    .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                    .ReturnsAsync([new Store { Id = "store-1" }]);

                return new AuthorizePaymentCommandHandler(
                    _orderService.Object,
                    new Mock<IPaymentService>().Object,
                    storeService.Object,
                    _manager.Object,
                    NullLogger<AuthorizePaymentCommandHandler>.Instance);
            }
        }

        private static AuthorizePaymentCommand Command(params (string Key, string Value)[] parameters)
        {
            return new AuthorizePaymentCommand
            {
                OrderId = "order-1",
                PaymentId = "payment-1",
                Parameters = parameters.Select(x => new KeyValue { Key = x.Key, Value = x.Value }).ToArray(),
            };
        }

        [Fact]
        public async Task Handle_PreparesTheRequestForTheOrdersCustomerBeforeThePaymentMethodRuns()
        {
            StoredPaymentContext context = null;
            _manager
                .Setup(x => x.PrepareAsync(It.IsAny<PostProcessPaymentRequest>(), It.IsAny<StoredPaymentContext>()))
                .Callback<PostProcessPaymentRequest, StoredPaymentContext>((request, ctx) =>
                {
                    context = ctx;
                    request.StoreForFutureUse = true;
                })
                .ReturnsAsync(StoredPaymentMethodOperationResult.Success());

            var result = await Handler.Handle(Command((StoredCredentialParameterNames.StoreForFutureUse, "true")), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            context.CustomerId.Should().Be("customer-1");
            context.StoreId.Should().Be("store-1");
            context.OrganizationId.Should().Be("organization-1");
            context.PaymentMethodCode.Should().Be(RecordingPaymentMethod.MethodCode);
            context.OrderId.Should().Be("order-1");
            context.PaymentId.Should().Be("payment-1");
            _paymentMethod.Requests.Should().ContainSingle().Which.StoreForFutureUse.Should().BeTrue("the payment method receives the typed request");
        }

        [Fact]
        public async Task Handle_CardTheCustomerCannotUse_IsRefusedWithoutCallingThePaymentMethod()
        {
            _manager
                .Setup(x => x.PrepareAsync(It.IsAny<PostProcessPaymentRequest>(), It.IsAny<StoredPaymentContext>()))
                .ReturnsAsync(StoredPaymentMethodOperationResult.Fail("This saved card can't be used. Choose another card."));

            var result = await Handler.Handle(Command((StoredCredentialParameterNames.StoredPaymentMethodId, "someone-elses-card")), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("This saved card can't be used. Choose another card.");
            _paymentMethod.Requests.Should().BeEmpty();
            _manager.Verify(x => x.SaveFromPaymentAsync(It.IsAny<IStoredCredentialPaymentRequest>(), It.IsAny<IStoredCredentialPaymentResult>(), It.IsAny<StoredPaymentContext>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ApprovedPayment_RecordsTheCardThePaymentMethodSaved()
        {
            var saved = new StoredPaymentMethod { Token = "token-1" };
            _paymentMethod.Result = new PostProcessPaymentRequestResult { IsSuccess = true, NewPaymentStatus = PaymentStatus.Authorized, StoredPaymentMethod = saved };

            var result = await Handler.Handle(Command(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _orderService.Verify(x => x.SaveChangesAsync(It.IsAny<IList<CustomerOrder>>()), Times.Once);
            _manager.Verify(x => x.SaveFromPaymentAsync(
                It.Is<IStoredCredentialPaymentRequest>(r => ReferenceEquals(r, _paymentMethod.Requests[0])),
                It.Is<IStoredCredentialPaymentResult>(r => ReferenceEquals(r.StoredPaymentMethod, saved)),
                It.Is<StoredPaymentContext>(c => c.OrderId == "order-1")), Times.Once);
        }

        [Fact]
        public async Task Handle_DeclinedPayment_RecordsNoCard()
        {
            _paymentMethod.Result = new PostProcessPaymentRequestResult { IsSuccess = false, ErrorMessage = "Declined" };

            var result = await Handler.Handle(Command(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            _manager.Verify(x => x.SaveFromPaymentAsync(It.IsAny<IStoredCredentialPaymentRequest>(), It.IsAny<IStoredCredentialPaymentResult>(), It.IsAny<StoredPaymentContext>()), Times.Never);
        }

        [Fact]
        public async Task Handle_RecordingTheCardFails_StillReportsTheApprovedPayment()
        {
            _paymentMethod.Result = new PostProcessPaymentRequestResult { IsSuccess = true, NewPaymentStatus = PaymentStatus.Authorized, StoredPaymentMethod = new StoredPaymentMethod() };
            _manager
                .Setup(x => x.SaveFromPaymentAsync(It.IsAny<IStoredCredentialPaymentRequest>(), It.IsAny<IStoredCredentialPaymentResult>(), It.IsAny<StoredPaymentContext>()))
                .ThrowsAsync(new System.InvalidOperationException("database unavailable"));

            var result = await Handler.Handle(Command(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue("the payment was approved and saved before the card");
        }

        private sealed class RecordingPaymentMethod : PaymentMethod
        {
            public const string MethodCode = "RecordingPaymentMethod";

            public RecordingPaymentMethod() : base(MethodCode)
            {
            }

            public override PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;
            public override PaymentMethodGroupType PaymentMethodGroupType => PaymentMethodGroupType.BankCard;

            public List<PostProcessPaymentRequest> Requests { get; } = [];

            public PostProcessPaymentRequestResult Result { get; set; } = new() { IsSuccess = true, NewPaymentStatus = PaymentStatus.Authorized };

            public override Task<ValidatePostProcessRequestResult> ValidatePostProcessRequestAsync(NameValueCollection queryString, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ValidatePostProcessRequestResult { IsSuccess = true });
            }

            public override Task<PostProcessPaymentRequestResult> PostProcessPaymentAsync(PostProcessPaymentRequest request, CancellationToken cancellationToken = default)
            {
                Requests.Add(request);
                return Task.FromResult(Result);
            }
        }
    }
}
