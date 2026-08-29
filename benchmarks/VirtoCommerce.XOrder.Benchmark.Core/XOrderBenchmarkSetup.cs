using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Benchmark;
using VirtoCommerce.XOrder.Core.Commands;

namespace VirtoCommerce.XOrder.Benchmark;

/// <summary>
/// The upstream setup: un-extended XOrder over an un-extended cart. A consuming module ships its own
/// <see cref="IOrderBenchmarkSetup"/> instead, and the same benchmark then measures its graph — which is
/// what makes the two runs comparable.
/// </summary>
public sealed class XOrderBenchmarkSetup : IOrderBenchmarkSetup
{
    public ICartBenchmarkSetup CreateCartSetup() => new XCartBenchmarkSetup();

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public CreateOrderFromCartCommand CreateCommand(string cartId) => new(cartId);
}
