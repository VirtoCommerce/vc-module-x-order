using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AutoMapper;
using MediatR;
using Moq;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.MarketingModule.Core.Search;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.OrdersModule.Data.Services;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Benchmark;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Core.Validators;
using VirtoCommerce.XCart.Data.Services;
using VirtoCommerce.XOrder.Core;
using VirtoCommerce.XOrder.Data.Commands;
using VirtoCommerce.XOrder.Data.Services;

namespace VirtoCommerce.XOrder.Benchmark;

/// <summary>
/// Fixture builders for the createOrderFromCart benchmark. The whole real graph runs — real
/// CustomerOrderBuilder (cart→order conversion), real order/cart aggregate repositories, real
/// totals calculator — with only the I/O leaves mocked (DB writes no-op, cart load via mediator).
///
/// The cart graph itself is built by the shared <see cref="CartBenchmarkFixtures"/> from the XCart
/// benchmark Core library, so the order benchmark's seed cart matches the cart benchmarks' shape
/// (same line items, configuration items, currency, store) without duplicating cart-graph code.
/// Only the order-specific harness — order builder, order/cart repositories, validation-context
/// factory, mediator — lives here.
/// </summary>
internal static class OrderBenchmarkFixtures
{
    private static Mock<ICurrencyService> CurrencyServiceMock()
    {
        var mock = new Mock<ICurrencyService>();
        mock.Setup(x => x.GetAllCurrenciesAsync()).ReturnsAsync([CartBenchmarkFixtures.Currency]);
        return mock;
    }

    private static Mock<IStoreService> StoreServiceMock()
    {
        var mock = new Mock<IStoreService>();
        mock.Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([CartBenchmarkFixtures.CreateStore()]); // GetByIdAsync extension delegates to GetAsync
        return mock;
    }

    private static Mock<IFileUploadService> FileUploadServiceMock()
    {
        // CustomerOrderAggregateRepository.UpdateConfigurationFiles calls the GetByPublicUrlAsync
        // extension unconditionally; it delegates to GetAsync, which a loose mock leaves null →
        // NRE on the following .Where. Return an empty list.
        var mock = new Mock<IFileUploadService>();
        mock.Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<File>());
        return mock;
    }

    /// <summary>Mutable holder: the mediator returns <see cref="CurrentCart"/>, which the benchmark
    /// rebuilds each iteration (createOrderFromCart consumes the cart, so it can't be shared).</summary>
    public sealed class OrderHarness
    {
        public CreateOrderFromCartCommandHandler Handler { get; set; } = null!;
        public CartAggregate CurrentCart { get; set; } = null!;
    }

    /// <summary>
    /// Builds a fresh, valid, recalculated checkout cart with <paramref name="itemCount"/> selected
    /// line items of the given <paramref name="shape"/> — so the handler's ValidateCart passes and
    /// conversion has real data. The cart graph (line items, configuration items) comes from the
    /// shared <see cref="CartBenchmarkFixtures"/>; the aggregate is the upstream cart aggregate with
    /// the real totals calculator and mocked I/O leaves.
    /// </summary>
    public static CartAggregate CreateCartAggregate(int itemCount, CartShape shape)
    {
        // Conversion does not map via the cart aggregate, so a mock mapper is enough here.
        var aggregate = CartBenchmarkFixtures.CreateAggregate(Mock.Of<IMapper>());
        var cart = CartBenchmarkFixtures.CreateCart(itemCount, shape);

        aggregate.GrabCart(cart, CartBenchmarkFixtures.CreateStore(), member: null, CartBenchmarkFixtures.Currency);

        // Settle totals before conversion (sync — IterationSetup cannot await). Validation reads
        // the products from the context (built in CreateHarness), not from aggregate.CartProducts,
        // so the dict is intentionally left empty.
        aggregate.RecalculateAsync().GetAwaiter().GetResult();

        return aggregate;
    }

    /// <summary>
    /// Wires the real <see cref="CreateOrderFromCartCommandHandler"/>. The mediator returns
    /// <see cref="OrderHarness.CurrentCart"/> (rebuilt each iteration); the validation-context
    /// factory wraps the aggregate's own products so ValidateCart's per-item rules pass.
    /// </summary>
    public static OrderHarness CreateHarness()
    {
        var harness = new OrderHarness();

        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Send(It.IsAny<GetCartByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => harness.CurrentCart);

        var orderBuilder = new CustomerOrderBuilder(
            Mock.Of<ICustomerOrderService>(),     // SaveChangesAsync no-op (DB write dropped)
            Mock.Of<ISettingsManager>(),          // ConvertCartToOrder doesn't read settings
            Mock.Of<IPaymentMethodsSearchService>());

        var orderRepository = new CustomerOrderAggregateRepository(
            customerOrderAggregateFactory: () =>
                new CustomerOrderAggregate(Mock.Of<IDynamicPropertyUpdaterService>(), Mock.Of<IPromotionUsageSearchService>()),
            customerOrderService: Mock.Of<ICustomerOrderService>(),
            currencyService: CurrencyServiceMock().Object,
            customerOrderBuilder: orderBuilder,
            fileUploadService: FileUploadServiceMock().Object,
            storeService: StoreServiceMock().Object);

        // Used only for the cleanup SaveAsync (recalculate + no-op write); the load path is unused.
        var cartRepository = new CartAggregateRepository(
            cartAggregateFactory: () => CartBenchmarkFixtures.CreateAggregate(Mock.Of<IMapper>()),
            shoppingCartSearchService: Mock.Of<IShoppingCartSearchService>(),
            shoppingCartService: Mock.Of<IShoppingCartService>(),
            currencyService: CurrencyServiceMock().Object,
            memberResolver: Mock.Of<IMemberResolver>(),
            storeService: StoreServiceMock().Object,
            cartProductsService: Mock.Of<ICartProductService>(),
            platformMemoryCache: Mock.Of<IPlatformMemoryCache>(),
            fileUploadService: FileUploadServiceMock().Object);

        // Build AllCartProducts from the aggregate's own line items (active/buyable) so the
        // per-item ValidateCart rules pass. The handler passes the aggregate's CartProducts (empty
        // here), so derive the product list from the line items instead.
        var validationContextFactory = new Mock<ICartValidationContextFactory>();
        validationContextFactory
            .Setup(x => x.CreateValidationContextAsync(It.IsAny<CartAggregate>(), It.IsAny<IList<CartProduct>>()))
            .ReturnsAsync((CartAggregate aggregate, IList<CartProduct> _) =>
                new CartValidationContext
                {
                    CartAggregate = aggregate,
                    AllCartProducts = aggregate.LineItems.Select(li => CartBenchmarkFixtures.CreateCartProduct(li.ProductId)).ToList(),
                });

        harness.Handler = new CreateOrderFromCartCommandHandler(
            Mock.Of<IShoppingCartService>(),
            orderRepository,
            cartRepository,
            validationContextFactory.Object,
            Mock.Of<IMemberService>(),
            mediator.Object);

        return harness;
    }
}
