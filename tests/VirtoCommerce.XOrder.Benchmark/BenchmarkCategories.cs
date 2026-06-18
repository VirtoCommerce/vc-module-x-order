namespace VirtoCommerce.XOrder.Benchmark;

/// <summary>
/// Category tags for tiered gating. Per-PR CI runs <c>--anyCategories Tier1</c>; the nightly job
/// runs the full matrix. <c>createOrderFromCart</c> is Tier 2 (heavier, runs less often than the
/// cart hot path), but the tag lets a team promote it to per-PR if order creation becomes critical.
/// </summary>
public static class BenchmarkCategories
{
    public const string Tier1 = nameof(Tier1);
    public const string Tier2 = nameof(Tier2);
}
