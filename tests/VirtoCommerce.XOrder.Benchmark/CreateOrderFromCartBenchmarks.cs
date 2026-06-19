using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Benchmark;
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
/// region. That forces InvocationCount=1: the deterministic Allocated figure stays exact; only Mean
/// precision softens.
///
/// Two axes: the <b>shape</b> (Flat vs Configured — configured items carry a configuration-item set
/// that ConvertCartToOrder maps and the recalculates walk, so a configured-product regression shows
/// up here) and the cart-size count. Read the Allocated column across the rows and before/after a
/// change; 100 surfaces super-linear growth.
/// </summary>
[MemoryDiagnoser]
public class CreateOrderFromCartBenchmarks
{
    private OrderBenchmarkFixtures.OrderHarness _harness = null!;
    private readonly CreateOrderFromCartCommand _command = new(cartId: "benchmark-cart");

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _harness = OrderBenchmarkFixtures.CreateHarness();

    // A fresh cart per iteration — createOrderFromCart empties it, so it must be rebuilt before each
    // measured invocation. Synchronous by BDN contract; the rebuild cost is excluded from the result.
    [IterationSetup]
    public void IterationSetup() => _harness.CurrentCart = OrderBenchmarkFixtures.CreateCartAggregate(LineItemCount, Shape);

    [Benchmark]
    public Task<CustomerOrderAggregate> CreateOrderFromCart() => _harness.Handler.Handle(_command, CancellationToken.None);
}
