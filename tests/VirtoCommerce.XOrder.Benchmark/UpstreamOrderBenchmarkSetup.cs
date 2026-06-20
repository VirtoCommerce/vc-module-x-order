using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Benchmark;
using VirtoCommerce.XOrder.Core.Commands;

namespace VirtoCommerce.XOrder.Benchmark;

/// <summary>
/// Upstream (un-extended XOrder) setup for the shared order benchmark engine: the input cart is the
/// upstream cart graph, no order overrides are contributed (the base XOrder handler + builder +
/// aggregate the host registers ARE the measured subject), and the dispatched command is the base
/// <see cref="CreateOrderFromCartCommand"/>. A consuming module provides its own setup to compare.
/// </summary>
public sealed class UpstreamOrderBenchmarkSetup : IOrderModuleBenchmarkSetup
{
    public ICartModuleBenchmarkSetup CreateCartSetup() => new UpstreamCartBenchmarkSetup();

    // The host's base order wiring is the subject — no consumer overrides upstream.
    public void ConfigureServices(IServiceCollection services)
    {
    }

    public CreateOrderFromCartCommand CreateCommand(string cartId) => new(cartId);
}
