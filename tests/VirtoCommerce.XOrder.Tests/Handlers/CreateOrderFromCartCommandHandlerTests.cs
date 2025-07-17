using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoMapper;
using FluentAssertions;
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

            var validationContextMock = new Mock<ICartValidationContextFactory>();
            validationContextMock.Setup(x => x.CreateValidationContextAsync(It.IsAny<CartAggregate>(), It.IsAny<IList<CartProduct>>()))
                .ReturnsAsync(new CartValidationContext());

            var orderAggregateRepositoryMock = new Mock<ICustomerOrderAggregateRepository>();

            var memberService = new Mock<IMemberService>();

            var request = new CreateOrderFromCartCommand(cart.Id);

            // Act
            var handler = new CreateOrderFromCartCommandHandler(
                cartService.Object,
                orderAggregateRepositoryMock.Object,
                aggregationService.Object,
                validationContextMock.Object,
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
            customerAggrRep.Setup(x => x.CreateOrderFromCart(It.IsAny<ShoppingCart>()))
                .ReturnsAsync(new CustomerOrderAggregate(null, null));

            var cartAggregate = new CartAggregate(null, null, null, null, null, null, null, null, null, null);
            cartAggregate.GrabCart(cart, new Store(), new Contact(), new Currency());

            var cartAggrRepository = new Mock<ICartAggregateRepository>();
            cartAggrRepository.Setup(x => x.GetCartForShoppingCartAsync(It.IsAny<ShoppingCart>(), null))
                .ReturnsAsync(cartAggregate);

            var contextFactory = new Mock<ICartValidationContextFactory>();
            contextFactory.Setup(x => x.CreateValidationContextAsync(It.IsAny<CartAggregate>(), It.IsAny<IList<CartProduct>>()))
                .ReturnsAsync(new CartValidationContext());

            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(x => x.Send(It.IsAny<GetCartByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => { return cartAggregate; });

            var memberService = new Mock<IMemberService>();

            var handler = new CreateOrderFromCartCommandHandler(
                cartService.Object,
                customerAggrRep.Object,
                cartAggrRepository.Object,
                contextFactory.Object,
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

        private static CartAggregate GetCartAggregateMock(ShoppingCart cart)
        {
            var cartAggregate = new CartAggregate(
                Mock.Of<IMarketingPromoEvaluator>(),
                Mock.Of<IShoppingCartTotalsCalculator>(),
                new Mock<IOptionalDependency<ITaxProviderSearchService>>().Object,
                Mock.Of<ICartProductService>(),
                Mock.Of<IDynamicPropertyUpdaterService>(),
                Mock.Of<IMapper>(),
                Mock.Of<IMemberService>(),
                Mock.Of<IGenericPipelineLauncher>(),
                Mock.Of<IConfigurationItemValidator>(),
                Mock.Of<IFileUploadService>());

            var contact = new Contact()
            {
                Id = Guid.NewGuid().ToString(),
            };

            cartAggregate.GrabCart(cart, new Store(), contact, new Currency());

            return cartAggregate;
        }
    }
}
