using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using MediatR;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Core.Validators;
using VirtoCommerce.XOrder.Core;
using VirtoCommerce.XOrder.Core.Commands;
using VirtoCommerce.XOrder.Core.Services;

namespace VirtoCommerce.XOrder.Data.Commands
{
    public class CreateOrderFromCartCommandHandler : IRequestHandler<CreateOrderFromCartCommand, CustomerOrderAggregate>
    {
        private readonly IShoppingCartService _cartService;
        private readonly ICustomerOrderAggregateRepository _customerOrderAggregateRepository;
        private readonly ICartAggregateRepository _cartRepository;
        private readonly ICartValidationContextFactory _cartValidationContextFactory;
        private readonly IMemberService _memberService;

        public string ValidationRuleSet { get; set; } = "*";

        public CreateOrderFromCartCommandHandler(
            IShoppingCartService cartService,
            ICustomerOrderAggregateRepository customerOrderAggregateRepository,
            ICartAggregateRepository cartRepository,
            ICartValidationContextFactory cartValidationContextFactory,
            IMemberService memberService)
        {
            _cartService = cartService;
            _customerOrderAggregateRepository = customerOrderAggregateRepository;
            _cartRepository = cartRepository;
            _cartValidationContextFactory = cartValidationContextFactory;
            _memberService = memberService;
        }

        public virtual async Task<CustomerOrderAggregate> Handle(CreateOrderFromCartCommand request, CancellationToken cancellationToken)
        {
            var cart = await _cartService.GetByIdAsync(request.CartId);
            var cartAggregate = await _cartRepository.GetCartForShoppingCartAsync(cart);

            await UpdateCart(cartAggregate);
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

        private async Task UpdateCart(CartAggregate cartAggregate)
        {
            // remove unselected gifts before order create
            var unselectedGifts = cartAggregate.GiftItems.Where(x => !x.SelectedForCheckout).ToList();
            if (unselectedGifts.Count != 0)
            {
                unselectedGifts.ForEach(x => cartAggregate.Cart.Items.Remove(x));
            }

            // update organization name
            if (!string.IsNullOrEmpty(cartAggregate.Cart.OrganizationId))
            {
                var organization = await _memberService.GetByIdAsync(cartAggregate.Cart.OrganizationId);
                cartAggregate.Cart.OrganizationName = organization?.Name;
            }
        }

        protected virtual async Task ValidateCart(CartAggregate cartAggregate)
        {
            var context = await _cartValidationContextFactory.CreateValidationContextAsync(cartAggregate);
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
