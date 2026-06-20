namespace VirtoCommerce.XOrder.Benchmark;

/// <summary>
/// Upstream runner's concrete createOrderFromCart benchmark: the logic is inherited from
/// <see cref="CreateOrderFromCartBenchmarksBase"/> (Core library); this subclass only bakes the
/// upstream module setup so BenchmarkDotNet discovers and runs it in this exe.
/// </summary>
public class CreateOrderFromCartBenchmarks : CreateOrderFromCartBenchmarksBase
{
    protected override IOrderModuleBenchmarkSetup CreateSetup() => new UpstreamOrderBenchmarkSetup();
}
