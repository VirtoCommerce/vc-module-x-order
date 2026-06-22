using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Benchmark;
using VirtoCommerce.XOrder.Core.Commands;

namespace VirtoCommerce.XOrder.Benchmark;

/// <summary>
/// The default setup: the un-extended XOrder platform for the shared order benchmark engine. The input
/// cart is the stock cart graph, no order overrides are contributed (the base XOrder handler + builder +
/// aggregate the host registers ARE the measured subject), and the dispatched command is the base
/// <see cref="CreateOrderFromCartCommand"/>. A consuming module provides its own setup to compare.
/// </summary>
public sealed class XOrderBenchmarkSetup : IOrderBenchmarkSetup
{
    public ICartBenchmarkSetup CreateCartSetup() => new XCartBenchmarkSetup();

    // The host's base order wiring is the subject — no consumer overrides it.
    public void ConfigureServices(IServiceCollection services)
    {
    }

    public CreateOrderFromCartCommand CreateCommand(string cartId) => new(cartId);
}
