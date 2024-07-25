using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using MediatR;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Core.Validators;
using VirtoCommerce.XOrder.Core;
using VirtoCommerce.XOrder.Core.Commands;
using VirtoCommerce.XOrder.Core.Services;

namespace VirtoCommerce.XOrder.Data.Commands
{
    public class CreateOrderFromCartCommandHandler : IRequestHandler<CreateOrderFromCartCommand, CustomerOrderAggregate>
    {
        private readonly ICustomerOrderAggregateRepository _customerOrderAggregateRepository;
        private readonly ICartAggregateRepository _cartRepository;
        private readonly ICartValidationContextFactory _cartValidationContextFactory;
        private readonly IMediator _mediator;

        public string ValidationRuleSet { get; set; } = "*";

        public CreateOrderFromCartCommandHandler(
            IShoppingCartService cartService,
            ICustomerOrderAggregateRepository customerOrderAggregateRepository,
            ICartAggregateRepository cartRepository,
            ICartValidationContextFactory cartValidationContextFactory,
            IMediator mediator)
        {
            _customerOrderAggregateRepository = customerOrderAggregateRepository;
            _cartRepository = cartRepository;
            _cartValidationContextFactory = cartValidationContextFactory;
            _mediator = mediator;
        }

        public virtual async Task<CustomerOrderAggregate> Handle(CreateOrderFromCartCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await _mediator.Send(new GetCartByIdQuery { CartId = request.CartId }, cancellationToken);

            // remove unselected gifts before order create
            var unselectedGifts = cartAggregate.GiftItems.Where(x => !x.SelectedForCheckout).ToList();
            if (unselectedGifts.Count != 0)
            {
                unselectedGifts.ForEach(x => cartAggregate.Cart.Items.Remove(x));
            }

            await ValidateCart(cartAggregate);

            // need to check for unsaved gift items before creating an order and resave the cart, otherwise an exception will be thrown on order create
            var hasUnsavedGifts = cartAggregate.GiftItems.Any(x => x.Id == null);
            if (hasUnsavedGifts)
            {
                await _cartRepository.SaveAsync(cartAggregate);
            }

            var result = await _customerOrderAggregateRepository.CreateOrderFromCart(cartAggregate.Cart);

            // remove selected items after order create
            var selectedLineItemIds = cartAggregate.SelectedLineItems.Select(x => x.Id).ToArray();
            await cartAggregate.RemoveItemsAsync(selectedLineItemIds);

            // clear cart
            cartAggregate.Cart.Shipments?.Clear();
            cartAggregate.Cart.Payments?.Clear();
            cartAggregate.Cart.Coupons?.Clear();

            cartAggregate.Cart.PurchaseOrderNumber = string.Empty;
            cartAggregate.Cart.Comment = string.Empty;
            cartAggregate.Cart.Coupon = string.Empty;
            cartAggregate.Cart.DynamicProperties?.Clear();

            await _cartRepository.SaveAsync(cartAggregate);

            return result;
        }

        protected virtual async Task ValidateCart(CartAggregate cartAggregate)
        {
            var context = await _cartValidationContextFactory.CreateValidationContextAsync(cartAggregate, cartAggregate.CartProducts.Select(x => x.Value).ToList());
            await cartAggregate.ValidateAsync(context, ValidationRuleSet);

            var errors = cartAggregate.ValidationErrors;
            if (errors.Any())
            {
                var dictionary = errors.GroupBy(x => x.ErrorCode).ToDictionary(x => x.Key, x => x.Select(y => y.ErrorMessage).FirstOrDefault());
                throw new ExecutionError("The cart has validation errors", dictionary) { Code = Constants.ValidationErrorCode };
            }
        }
    }
}
