using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AutoMapper;
using MediatR;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CartModule.Data.Services;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.MarketingModule.Core.Search;
using VirtoCommerce.MarketingModule.Core.Services;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.OrdersModule.Data.Services;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.TaxModule.Core.Services;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.Xapi.Core.Services;
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
/// </summary>
internal static class OrderBenchmarkFixtures
{
    public const string StoreId = "benchmark-store";

    public static readonly Currency Currency = new(new Language("en-US"), "USD")
    {
        ExchangeRate = 1m,
        RoundingPolicy = new DefaultMoneyRoundingPolicy(),
    };

    /// <summary>Mutable holder: the mediator returns <see cref="CurrentCart"/>, which the benchmark
    /// rebuilds each iteration (createOrderFromCart consumes the cart, so it can't be shared).</summary>
    public sealed class OrderHarness
    {
        public CreateOrderFromCartCommandHandler Handler { get; set; } = null!;
        public CartAggregate CurrentCart { get; set; } = null!;
    }

    private static Store CreateStore() => new() { Id = StoreId, Settings = [] };

    private static Mock<ICurrencyService> CurrencyServiceMock()
    {
        var mock = new Mock<ICurrencyService>();
        mock.Setup(x => x.GetAllCurrenciesAsync()).ReturnsAsync([Currency]);
        return mock;
    }

    private static Mock<IStoreService> StoreServiceMock()
    {
        var mock = new Mock<IStoreService>();
        mock.Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([CreateStore()]); // GetByIdAsync extension delegates to GetAsync
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

    /// <summary>A bare cart aggregate with the real totals calculator and all other deps mocked.</summary>
    private static CartAggregate CreateBareAggregate()
    {
        var totalsCalculator = new DefaultShoppingCartTotalsCalculator(CurrencyServiceMock().Object);

        var marketingEvaluator = new Mock<IMarketingPromoEvaluator>();
        marketingEvaluator
            .Setup(x => x.EvaluatePromotionAsync(It.IsAny<PromotionEvaluationContext>()))
            .ReturnsAsync(new PromotionResult());

        // XCart 3.1001.0 (the version x-order ships against) has an 11-arg CartAggregate ctor —
        // no ICartValidationContextFactory (that arrived on a later XCart). Match the shipped API.
        return new CartAggregate(
            marketingEvaluator.Object,
            totalsCalculator,
            Mock.Of<IOptionalDependency<ITaxProviderSearchService>>(),
            Mock.Of<ICartProductService>(),
            Mock.Of<IDynamicPropertyUpdaterService>(),
            Mock.Of<IMapper>(), // conversion doesn't map via the cart aggregate
            Mock.Of<IMemberService>(),
            Mock.Of<IGenericPipelineLauncher>(),
            Mock.Of<IConfigurationItemValidator>(),
            Mock.Of<IFileUploadService>(),
            Mock.Of<ICartSharingService>());
    }

    private static CartProduct CreateCartProduct(string productId) =>
        new(new CatalogProduct
        {
            Id = productId,
            CatalogId = "catalog",
            Code = $"SKU-{productId}",
            Name = $"Product {productId}",
            IsActive = true,
            IsBuyable = true,
            TrackInventory = false,
        })
        {
            Price = new ProductPrice(Currency)
            {
                ListPrice = new Money(10m, Currency),
                SalePrice = new Money(9m, Currency),
            },
        };

    /// <summary>
    /// Builds a fresh, valid, recalculated checkout cart with <paramref name="itemCount"/> selected
    /// flat-SKU line items and matching active/buyable products — so the handler's ValidateCart
    /// passes and conversion has real data. No payments/shipments (the validator only checks those
    /// when present).
    /// </summary>
    public static CartAggregate CreateCartAggregate(int itemCount)
    {
        var aggregate = CreateBareAggregate();

        var items = new List<LineItem>(itemCount);
        for (var i = 0; i < itemCount; i++)
        {
            items.Add(new LineItem
            {
                Id = $"li-{i}",
                ProductId = $"product-{i}",
                CatalogId = "catalog",
                Sku = $"SKU-product-{i}",
                Name = $"Product {i}",
                Currency = Currency.Code,
                Quantity = 2,
                ListPrice = 10m,
                SalePrice = 9m,
                SelectedForCheckout = true,
            });
        }

        var cart = new ShoppingCart
        {
            Id = "benchmark-cart",
            Name = "default",
            StoreId = StoreId,
            CustomerId = "benchmark-user",
            Currency = Currency.Code,
            LanguageCode = "en-US",
            Items = items,
            Shipments = [],
            Payments = [],
        };

        aggregate.GrabCart(cart, CreateStore(), member: null, Currency);

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
            cartAggregateFactory: CreateBareAggregate,
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
                    AllCartProducts = aggregate.LineItems.Select(li => CreateCartProduct(li.ProductId)).ToList(),
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
