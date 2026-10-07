using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using FluentValidation.Internal;
using FluentValidation.Results;
using GraphQL;
using MediatR;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.MarketingModule.Core.Services;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.TaxModule.Core.Services;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Core.Validators;
using VirtoCommerce.XOrder.Core;
using VirtoCommerce.XOrder.Core.Commands;
using VirtoCommerce.XOrder.Core.Services;
using VirtoCommerce.XOrder.Data.Commands;
using VirtoCommerce.XOrder.Tests.Helpers;
using Xunit;
using Store = VirtoCommerce.StoreModule.Core.Model.Store;

namespace VirtoCommerce.XOrder.Tests.Handlers
{
    public class CreateOrderFromCartCommandHandlerTests : CustomerOrderMockHelper
    {
        [Fact]
        public Task Handle_CartHasValidationErrors_ExceptionThrown()
        {
            // Arrange
            var cart = _fixture.Create<ShoppingCart>();
            var lineItem = _fixture.Create<LineItem>();
            cart.Items = new List<LineItem>() { lineItem };

            var cartAggregate = GetCartAggregateMock(cart);
            var aggregationService = new Mock<ICartAggregateRepository>();
            var cartService = new Mock<IShoppingCartService>();

            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(x => x.Send(It.IsAny<GetCartByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    var error = CartErrorDescriber.ProductPriceChangedError(lineItem, lineItem.SalePrice, lineItem.SalePriceWithTax, 0, 0);
                    cartAggregate.ValidationWarnings.Add(error);

                    return cartAggregate;
                });

            var orderAggregateRepositoryMock = new Mock<ICustomerOrderAggregateRepository>();

            var memberService = new Mock<IMemberService>();

            var request = new CreateOrderFromCartCommand(cart.Id);

            // Act
            var handler = new CreateOrderFromCartCommandHandler(
                cartService.Object,
                orderAggregateRepositoryMock.Object,
                aggregationService.Object,
                memberService.Object,
                mediatorMock.Object);

            // Assert
            return Assert.ThrowsAsync<ExecutionError>(() => handler.Handle(request, CancellationToken.None));
        }

        /// <summary>
        /// To test if line items are deleted in the create order routine
        /// </summary>
        [Fact]
        public async Task Handle_CreateOrder_EnsureSelectedLineItemsDeleted()
        {
            // Arrange
            var cart = new ShoppingCart()
            {
                Name = "default",
                Currency = "USD",
                CustomerId = Guid.NewGuid().ToString(),
                Items = new List<LineItem>
                {
                    new LineItem()
                    {
                        Id = Guid.NewGuid().ToString(),
                        SelectedForCheckout = true,
                    },
                    new LineItem()
                    {
                        Id = Guid.NewGuid().ToString(),
                        SelectedForCheckout = false,
                    },
                }
            };

            var cartService = new Mock<IShoppingCartService>();

            var customerAggrRep = new Mock<ICustomerOrderAggregateRepository>();
            customerAggrRep
                .Setup(x => x.CreateOrderFromCart(It.IsAny<ShoppingCart>()))
                .ReturnsAsync(new CustomerOrderAggregate(null, null));

            var contextFactory = new Mock<ICartValidationContextFactory>();
            contextFactory
                .Setup(x => x.CreateValidationContextAsync(It.IsAny<CartAggregate>()))
                .ReturnsAsync(new CartValidationContext());

            var validatorRegistry = new Mock<ICartValidatorRegistry>();
            validatorRegistry
                .Setup(x => x.ValidateAsync(It.IsAny<CartValidationContext>(), It.IsAny<Action<ValidationStrategy<CartValidationContext>>>()))
                .ReturnsAsync(new List<ValidationFailure>());

            var cartAggregate = new CartAggregate(null, null, null, null, null, null, null, null, null, contextFactory.Object, Mock.Of<ICartItemBuilder>(), validatorRegistry.Object);
            cartAggregate.GrabCart(cart, new Store(), new Contact(), new Currency());

            var cartAggrRepository = new Mock<ICartAggregateRepository>();
            cartAggrRepository
                .Setup(x => x.GetCartForShoppingCartAsync(It.IsAny<ShoppingCart>(), null))
                .ReturnsAsync(cartAggregate);

            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(x => x.Send(It.IsAny<GetCartByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => { return cartAggregate; });

            var memberService = new Mock<IMemberService>();

            var handler = new CreateOrderFromCartCommandHandler(
                cartService.Object,
                customerAggrRep.Object,
                cartAggrRepository.Object,
                memberService.Object,
                mediatorMock.Object)
            {
                ValidationRuleSet = "default"
            };

            // Act
            await handler.Handle(new CreateOrderFromCartCommand(""), CancellationToken.None);

            // Assert
            cart.Items.Count.Should().Be(1);
        }

        /// <summary>
        /// A derived aggregate can post-process validation results in a ValidateAsync(string) override (VCST-6089).
        /// The obsolete CartValidationErrors mirror, which GetValidationErrors() reads, only ever holds the base
        /// result, so an error the override adds never reached the old gate. The gate must reject on the list
        /// ValidateAsync returns.
        /// </summary>
        [Fact]
        public async Task Handle_ValidateAsyncOverrideAddsError_ExceptionThrown()
        {
            // Arrange
            var cart = GetEmptyCart();
            var extraError = new ValidationFailure("Cart", "Rejected by a project rule") { ErrorCode = "PROJECT_RULE" };

            var cartAggregate = new ExtraErrorCartAggregate(GetValidationContextFactory(), GetValidatorRegistry(), extraError);
            cartAggregate.GrabCart(cart, new Store(), new Contact(), new Currency());

            var orderAggregateRepositoryMock = new Mock<ICustomerOrderAggregateRepository>();
            var handler = GetHandler(cartAggregate, orderAggregateRepositoryMock.Object);

            // Act
            var exception = await Assert.ThrowsAsync<ExecutionError>(() => handler.Handle(new CreateOrderFromCartCommand(cart.Id), CancellationToken.None));

            // Assert
            exception.Data.Contains("PROJECT_RULE").Should().BeTrue();
            orderAggregateRepositoryMock.Verify(x => x.CreateOrderFromCart(It.IsAny<ShoppingCart>()), Times.Never);
        }

        /// <summary>
        /// Errors recorded by cart operations (OperationValidationErrors) refuse the order even when the validated
        /// ruleSet itself is clean: the gate adds them to the ValidateAsync result, as GetValidationErrors() did.
        /// </summary>
        [Fact]
        public async Task Handle_OperationValidationErrors_ExceptionThrown()
        {
            // Arrange
            var cart = GetEmptyCart();

            var cartAggregate = GetCartAggregate(cart, GetValidatorRegistry());
            cartAggregate.OperationValidationErrors.Add(new ValidationFailure("LineItem", "The product is no longer available") { ErrorCode = "CART_PRODUCT_UNAVAILABLE" });

            var orderAggregateRepositoryMock = new Mock<ICustomerOrderAggregateRepository>();
            var handler = GetHandler(cartAggregate, orderAggregateRepositoryMock.Object);

            // Act
            var exception = await Assert.ThrowsAsync<ExecutionError>(() => handler.Handle(new CreateOrderFromCartCommand(cart.Id), CancellationToken.None));

            // Assert
            exception.Data.Contains("CART_PRODUCT_UNAVAILABLE").Should().BeTrue();
            orderAggregateRepositoryMock.Verify(x => x.CreateOrderFromCart(It.IsAny<ShoppingCart>()), Times.Never);
        }

        private static CartAggregate GetCartAggregateMock(ShoppingCart cart)
        {
            // Registry reports a cart-level validation failure, which ValidateAsync returns: the trigger
            // for CreateOrderFromCartCommandHandler.ValidateCart to throw ExecutionError.
            var validatorRegistry = GetValidatorRegistry(new ValidationFailure("Cart", "Cart has validation errors") { ErrorCode = "CART_HAS_ERRORS" });

            return GetCartAggregate(cart, validatorRegistry);
        }

        private static ShoppingCart GetEmptyCart()
        {
            return new ShoppingCart
            {
                Name = "default",
                Currency = "USD",
                CustomerId = Guid.NewGuid().ToString(),
                Items = new List<LineItem>(),
            };
        }

        private static ICartValidatorRegistry GetValidatorRegistry(params ValidationFailure[] errors)
        {
            var validatorRegistry = new Mock<ICartValidatorRegistry>();
            validatorRegistry
                .Setup(x => x.ValidateAsync(It.IsAny<CartValidationContext>(), It.IsAny<Action<ValidationStrategy<CartValidationContext>>>()))
                .ReturnsAsync(new List<ValidationFailure>(errors));

            return validatorRegistry.Object;
        }

        private static ICartValidationContextFactory GetValidationContextFactory()
        {
            var validationContextFactory = new Mock<ICartValidationContextFactory>();
            validationContextFactory
                .Setup(x => x.CreateValidationContextAsync(It.IsAny<CartAggregate>()))
                .ReturnsAsync(new CartValidationContext());

            return validationContextFactory.Object;
        }

        private static CreateOrderFromCartCommandHandler GetHandler(CartAggregate cartAggregate, ICustomerOrderAggregateRepository orderAggregateRepository)
        {
            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(x => x.Send(It.IsAny<GetCartByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cartAggregate);

            return new CreateOrderFromCartCommandHandler(
                Mock.Of<IShoppingCartService>(),
                orderAggregateRepository,
                Mock.Of<ICartAggregateRepository>(),
                Mock.Of<IMemberService>(),
                mediatorMock.Object);
        }

        private static CartAggregate GetCartAggregate(ShoppingCart cart, ICartValidatorRegistry validatorRegistry)
        {
            var cartAggregate = new CartAggregate(
                Mock.Of<IMarketingPromoEvaluator>(),
                Mock.Of<IShoppingCartTotalsCalculator>(),
                new Mock<IOptionalDependency<ITaxProviderSearchService>>().Object,
                Mock.Of<ICartProductService>(),
                Mock.Of<IDynamicPropertyUpdaterService>(),
                Mock.Of<IXCartMapper>(),
                Mock.Of<IMemberService>(),
                Mock.Of<IGenericPipelineLauncher>(),
                Mock.Of<IFileUploadService>(),
                GetValidationContextFactory(),
                Mock.Of<ICartItemBuilder>(),
                validatorRegistry);

            var contact = new Contact()
            {
                Id = Guid.NewGuid().ToString(),
            };

            cartAggregate.GrabCart(cart, new Store(), contact, new Currency());

            return cartAggregate;
        }

        /// <summary>
        /// Post-processes validation the way a project's derived aggregate would: the list it returns differs from
        /// the base result, which is all the obsolete CartValidationErrors mirror ever holds.
        /// </summary>
        private sealed class ExtraErrorCartAggregate : CartAggregate
        {
            private readonly ValidationFailure _extraError;

            public ExtraErrorCartAggregate(ICartValidationContextFactory validationContextFactory, ICartValidatorRegistry validatorRegistry, ValidationFailure extraError)
                : base(null, null, null, null, null, null, null, null, null, validationContextFactory, Mock.Of<ICartItemBuilder>(), validatorRegistry)
            {
                _extraError = extraError;
            }

            public override async Task<IList<ValidationFailure>> ValidateAsync(string ruleSet)
            {
                var errors = await base.ValidateAsync(ruleSet);

                // A new list: the one base returns is the cached result, so it must not be changed in place.
                return new List<ValidationFailure>(errors) { _extraError };
            }
        }
    }
}
