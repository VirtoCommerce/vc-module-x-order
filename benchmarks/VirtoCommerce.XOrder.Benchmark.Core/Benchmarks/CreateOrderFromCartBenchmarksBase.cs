using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Benchmark;
using VirtoCommerce.XOrder.Core;

namespace VirtoCommerce.XOrder.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>createOrderFromCart</c> GraphQL mutation
/// (<see cref="VirtoCommerce.XOrder.Data.Commands.CreateOrderFromCartCommandHandler.Handle"/>). The
/// logic lives here in the Core library so a consumer can run the same benchmark under its own
/// <see cref="IOrderBenchmarkSetup"/>.
///
/// <para>The handler CONSUMES the cart it converts, so a number is only real if each invocation got its
/// own — <c>[IterationSetup]</c> supplies one per iteration, and that is enough only at one invocation
/// per iteration. The resolved job line is not the check (Dry's never mentions <c>InvocationCount</c>);
/// the tell that holds at every job is <c>1 op</c> in each <c>WorkloadActual</c> row. Three edits break
/// the invariant while still printing a plausible number: <c>InvocationCount</c> above 1,
/// <c>UnrollFactor</c> set on its own, and removing the iteration setup.</para>
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

    // A fresh cart per iteration — createOrderFromCart empties it, so it must be rebuilt. This must
    // BLOCK: BenchmarkDotNet assigns the method to an Action, so an unawaited rebuild still compiles
    // and then measures the handler racing its own cart refresh.
    [IterationSetup]
    public void IterationSetup() => _harness.RefreshCart();

    [Benchmark]
    public Task<CustomerOrderAggregate> CreateOrderFromCart() => _harness.SendAsync();
}
