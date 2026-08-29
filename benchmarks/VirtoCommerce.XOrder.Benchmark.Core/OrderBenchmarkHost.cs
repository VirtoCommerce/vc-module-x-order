using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
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
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Benchmark;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XOrder.Core;
using VirtoCommerce.XOrder.Core.Commands;
using VirtoCommerce.XOrder.Core.Services;
using VirtoCommerce.XOrder.Data.Services;

namespace VirtoCommerce.XOrder.Benchmark;

/// <summary>
/// Builds the benchmark harness for the createOrderFromCart command the same way the order module is
/// wired in production: the command/query <b>handler</b> (and a consumer's override of it) is resolved
/// through MediatR exactly as it ships. The single design rule matches the cart benchmarks: everything
/// that does I/O is a mock, everything that is pure compute runs for real.
///
/// <para>The input cart comes from <see cref="CartBenchmarkHost"/> via the setup's
/// <see cref="IOrderBenchmarkSetup.CreateCartSetup"/>, so it is loaded + recalculated exactly as
/// the cart benchmarks build it (including a consumer's real cart graph). The order machinery (builder,
/// aggregate, repository) is registered base-first, then the setup's
/// <see cref="IOrderBenchmarkSetup.ConfigureServices"/> applies its overrides last (DI
/// last-registration wins). The benchmark dispatches <see cref="IOrderBenchmarkSetup.CreateCommand"/>
/// through MediatR, so the consumer's overridden command type routes to its handler.</para>
/// </summary>
public static class OrderBenchmarkHost
{
    public const string CartId = "benchmark-cart";

    /// <summary>
    /// Composes the order benchmark harness for a cart of <paramref name="lineItemCount"/> items of the
    /// given <paramref name="shape"/>. The input cart is loaded fresh from the cart repository on every
    /// <see cref="OrderHarness.RefreshCart"/>.
    /// </summary>
    public static OrderHarness BuildHarness(IOrderBenchmarkSetup setup, int lineItemCount, CartShape shape)
    {
        // Input cart provider — reuses the whole cart benchmark wiring (incl. a consumer's cart graph via
        // the CreateCart hook). GetCartByIdAsync returns a freshly loaded + recalculated aggregate.
        var cartProvider = CartBenchmarkHost.BuildProvider(setup.CreateCartSetup(), lineItemCount, shape);
        var cartRepository = cartProvider.GetRequiredService<ICartAggregateRepository>();

        var harness = new OrderHarness
        {
            CartRepository = cartRepository,
            Command = setup.CreateCommand(CartId),
        };

        var services = new ServiceCollection();

        // Base handlers — register MediatR from the Core AND Data assemblies, mirroring the module's
        // production AddSchema(typeof(CoreAssemblyMarker), typeof(DataAssemblyMarker)). Anchor on the
        // assembly marker types, not specific handlers that could be renamed/removed.
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
            typeof(Core.CoreAssemblyMarker).Assembly,
            typeof(Data.DataAssemblyMarker).Assembly));

        services.AddSingleton<IRequestHandler<GetCartByIdQuery, CartAggregate>>(new CurrentCartQueryHandler(harness));

        // Cleanup save (recalculate + no-op DB write) reuses the cart provider's repository, so the
        // cleanup recalculate runs through the same (consumer) cart wiring as the input load.
        services.AddSingleton(cartRepository);

        // ── Mocked I/O leaves ─────────────────────────────────────────────────────────────────────
        services.AddSingleton(Mock.Of<IShoppingCartService>());
        services.AddSingleton(Mock.Of<IMemberService>());
        services.AddSingleton(Mock.Of<ICustomerOrderService>());
        services.AddSingleton(Mock.Of<ISettingsManager>()); // ConvertCartToOrder reads the initial order + line-item status
        services.AddSingleton(Mock.Of<IPaymentMethodsSearchService>());
        services.AddSingleton(Mock.Of<IDynamicPropertyUpdaterService>());
        services.AddSingleton(Mock.Of<IPromotionUsageSearchService>());
        services.AddSingleton(CurrencyServiceMock());
        services.AddSingleton(StoreServiceMock());
        services.AddSingleton(FileUploadServiceMock());
        // No ICartValidationContextFactory here: the cart aggregate this handler validates is built by
        // CartBenchmarkHost's container, so that host's registration is the one it resolves — a copy in
        // this collection would look like it configured validation while changing nothing.

        // ── Order machinery (base; a consumer setup overrides builder / aggregate / repository) ──────
        services.AddTransient<ICustomerOrderBuilder, CustomerOrderBuilder>();
        services.AddTransient<CustomerOrderAggregate>();
        services.AddTransient<Func<CustomerOrderAggregate>>(sp => sp.GetRequiredService<CustomerOrderAggregate>);
        services.AddTransient<ICustomerOrderAggregateRepository, CustomerOrderAggregateRepository>();

        setup.ConfigureServices(services);

        harness.Mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        return harness;
    }

    private static ICurrencyService CurrencyServiceMock()
    {
        var mock = new Mock<ICurrencyService>();
        mock.Setup(x => x.GetAllCurrenciesAsync()).ReturnsAsync([CartBenchmarkFixtures.Currency]);

        return mock.Object;
    }

    private static IStoreService StoreServiceMock()
    {
        var mock = new Mock<IStoreService>();
        mock.Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([CartBenchmarkFixtures.CreateStore()]); // GetByIdAsync extension delegates to GetAsync

        return mock.Object;
    }

    private static IFileUploadService FileUploadServiceMock()
    {
        // CustomerOrderAggregateRepository.UpdateConfigurationFiles calls the GetByPublicUrlAsync
        // extension unconditionally; it delegates to GetAsync, which a loose mock leaves null → NRE on
        // the following .Where. Return an empty (non-null) list.
        var mock = new Mock<IFileUploadService>();
        mock.Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<File>());

        return mock.Object;
    }

    /// <summary>Mutable harness: the registered <see cref="GetCartByIdQuery"/> handler returns
    /// <see cref="CurrentCart"/>, which <see cref="RefreshCart"/> rebuilds each iteration.</summary>
    public sealed class OrderHarness
    {
        public IMediator Mediator { get; set; }
        public ICartAggregateRepository CartRepository { get; init; }
        public CreateOrderFromCartCommand Command { get; init; }
        public CartAggregate CurrentCart { get; private set; }

        public void RefreshCart() =>
            CurrentCart = CartRepository.GetCartByIdAsync(CartId).GetAwaiter().GetResult();

        public Task<CustomerOrderAggregate> SendAsync() => Mediator.Send(Command);
    }

    private sealed class CurrentCartQueryHandler(OrderHarness harness) : IRequestHandler<GetCartByIdQuery, CartAggregate>
    {
        public Task<CartAggregate> Handle(GetCartByIdQuery request, CancellationToken cancellationToken) =>
            Task.FromResult(harness.CurrentCart);
    }
}
