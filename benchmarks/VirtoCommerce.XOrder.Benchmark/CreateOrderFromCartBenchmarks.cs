namespace VirtoCommerce.XOrder.Benchmark;

/// <summary>
/// BenchmarkDotNet discovers benchmarks only in the runner's own assembly, so the abstract
/// <see cref="CreateOrderFromCartBenchmarksBase"/> needs a concrete subclass here to run at all —
/// deleting this apparently-empty class yields a run with no cases.
/// </summary>
public class CreateOrderFromCartBenchmarks : CreateOrderFromCartBenchmarksBase
{
    protected override IOrderBenchmarkSetup CreateSetup() => new XOrderBenchmarkSetup();
}
