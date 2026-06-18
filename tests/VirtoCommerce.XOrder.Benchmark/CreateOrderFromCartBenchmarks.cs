using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XOrder.Core;
using VirtoCommerce.XOrder.Core.Commands;

namespace VirtoCommerce.XOrder.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>createOrderFromCart</c> GraphQL mutation
/// (<see cref="VirtoCommerce.XOrder.Data.Commands.CreateOrderFromCartCommandHandler.Handle"/>). The
/// measured compute = cart validation + cart→order conversion + two cart recalculates (the cleanup
/// save), all real; only DB writes are no-op mocks and the cart load comes from a mocked mediator.
///
/// createOrderFromCart consumes the cart (cleanup removes the items), so it is NOT idempotent across
/// invocations — each iteration rebuilds a fresh cart in [IterationSetup], outside the measured
/// region. That forces InvocationCount=1: the Allocated gate stays exact; only Mean precision softens.
///
/// Tier 2 (heavier, run nightly). The cart size param doubles as scale + superlinearity canary at 100.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(BenchmarkCategories.Tier2)]
public class CreateOrderFromCartBenchmarks
{
    private OrderBenchmarkFixtures.OrderHarness _harness = null!;
    private readonly CreateOrderFromCartCommand _command = new(cartId: "benchmark-cart");

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup() => _harness = OrderBenchmarkFixtures.CreateHarness();

    // A fresh cart per iteration — createOrderFromCart empties it, so it must be rebuilt before each
    // measured invocation. Synchronous by BDN contract; the rebuild cost is excluded from the result.
    [IterationSetup]
    public void IterationSetup() => _harness.CurrentCart = OrderBenchmarkFixtures.CreateCartAggregate(LineItemCount);

    [Benchmark]
    public Task<CustomerOrderAggregate> CreateOrderFromCart() => _harness.Handler.Handle(_command, CancellationToken.None);
}
