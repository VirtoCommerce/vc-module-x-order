using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Benchmark;
using VirtoCommerce.XOrder.Core;

namespace VirtoCommerce.XOrder.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>createOrderFromCart</c> GraphQL mutation
/// (<see cref="VirtoCommerce.XOrder.Data.Commands.CreateOrderFromCartCommandHandler.Handle"/>). The
/// measured compute = cart validation + cart→order conversion + one cart recalculate (the cleanup
/// save), all real; only DB writes are no-op mocks and the cart load is served from the harness.
///
/// <para>The benchmark <b>logic</b> lives here in the Core library; each runner exe defines a concrete
/// subclass overriding <see cref="CreateSetup"/> to bake its module setup (upstream / a consumer), so
/// the SAME benchmark runs against the un-extended XOrder handler or a consumer's overridden handler +
/// order builder + aggregate over a consumer cart graph — a consumer run is then directly comparable.</para>
///
/// <para>The handler CONSUMES the cart it converts, so a number is only real if each invocation got its
/// own. <c>[IterationSetup]</c> supplies one per iteration; one invocation per iteration is what makes
/// that enough, and both stock jobs reach it by different routes — <c>Short</c> and above because
/// BenchmarkDotNet pins <c>InvocationCount=1</c> for a benchmark with an iteration setup on a job that
/// pins neither <c>InvocationCount</c> nor <c>UnrollFactor</c>, <c>Dry</c> because <c>ColdStart</c> skips
/// the pilot. So the job line is NOT the check: Dry's does not mention <c>InvocationCount</c> at all.
/// The tell that holds everywhere is <c>1 op</c> in every <c>WorkloadActual</c> row.</para>
///
/// <para>Three edits break it silently, all leaving a plausible number: setting <c>InvocationCount</c>
/// above 1, setting <c>UnrollFactor</c> alone (which suppresses the pin, and Throughput's pilot then
/// chooses a count above 1), and removing the iteration setup.</para>
///
/// <para>Two axes: the cart <b>shape</b> (Flat vs Configured — configured items carry a configuration
/// set that ConvertCartToOrder maps and the recalculate walks) and the cart size.</para>
/// </summary>
[MemoryDiagnoser]
public abstract class CreateOrderFromCartBenchmarksBase
{
    private OrderBenchmarkHost.OrderHarness _harness = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    /// <summary>The module setup the concrete runner subclass bakes in — upstream, or a consumer's.</summary>
    protected abstract IOrderBenchmarkSetup CreateSetup();

    [GlobalSetup]
    public void Setup() => _harness = OrderBenchmarkHost.BuildHarness(CreateSetup(), LineItemCount, Shape);

    // A fresh cart per iteration — createOrderFromCart empties it, so it must be rebuilt. Synchronous by
    // BDN contract; the rebuild cost is excluded from the result.
    [IterationSetup]
    public void IterationSetup() => _harness.RefreshCart();

    [Benchmark]
    public Task<CustomerOrderAggregate> CreateOrderFromCart() => _harness.SendAsync();
}
