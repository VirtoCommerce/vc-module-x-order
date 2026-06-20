using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Benchmark;
using VirtoCommerce.XOrder.Core;

namespace VirtoCommerce.XOrder.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>createOrderFromCart</c> GraphQL mutation
/// (<see cref="VirtoCommerce.XOrder.Data.Commands.CreateOrderFromCartCommandHandler.Handle"/>). The
/// measured compute = cart validation + cart→order conversion + two cart recalculates (the cleanup
/// save), all real; only DB writes are no-op mocks and the cart load is served from the harness.
///
/// <para>The benchmark <b>logic</b> lives here in the Core library; each runner exe defines a concrete
/// subclass overriding <see cref="CreateSetup"/> to bake its module setup (upstream / a consumer), so
/// the SAME benchmark runs against the un-extended XOrder handler or a consumer's overridden handler +
/// order builder + aggregate over a consumer cart graph — a LEO run is then directly comparable.</para>
///
/// <para>createOrderFromCart consumes the cart (cleanup removes the items), so it is NOT idempotent —
/// each iteration rebuilds a fresh cart in <c>[IterationSetup]</c>, outside the measured region. That
/// forces <c>InvocationCount=1</c>: the deterministic Allocated figure stays exact; only Mean precision
/// softens.</para>
///
/// <para>Two axes: the cart <b>shape</b> (Flat vs Configured — configured items carry a configuration
/// set that ConvertCartToOrder maps and the recalculates walk) and the cart-size count; 100 surfaces
/// super-linear growth.</para>
/// </summary>
[MemoryDiagnoser]
public abstract class CreateOrderFromCartBenchmarksBase
{
    private OrderBenchmarkHost.OrderHarness _harness = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    /// <summary>The module setup baked by the concrete runner subclass (upstream / a consumer).</summary>
    protected abstract IOrderModuleBenchmarkSetup CreateSetup();

    [GlobalSetup]
    public void Setup() => _harness = OrderBenchmarkHost.BuildHarness(CreateSetup(), LineItemCount, Shape);

    // A fresh cart per iteration — createOrderFromCart empties it, so it must be rebuilt before each
    // measured invocation. Synchronous by BDN contract; the rebuild cost is excluded from the result.
    [IterationSetup]
    public void IterationSetup() => _harness.RefreshCart();

    [Benchmark]
    public Task<CustomerOrderAggregate> CreateOrderFromCart() => _harness.SendAsync();
}
