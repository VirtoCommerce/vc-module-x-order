using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Benchmark;
using VirtoCommerce.XOrder.Core.Commands;

namespace VirtoCommerce.XOrder.Benchmark;

/// <summary>
/// The module-agnostic seam that lets the same createOrderFromCart benchmark run against the
/// un-extended XOrder platform or against a consuming module that overrides the order graph
/// (a subclassed order aggregate, a custom order builder, an overridden command + handler). A setup
/// answers the three questions that differ per module: which cart to convert, which registrations to
/// contribute to the order DI container, and which command to dispatch.
/// </summary>
public interface IOrderModuleBenchmarkSetup
{
    /// <summary>
    /// The cart setup that builds the <b>input</b> cart the order is created from. The order host loads
    /// it through <see cref="CartBenchmarkHost"/>, so the cart is recalculated exactly as the cart
    /// benchmarks build it — upstream returns <see cref="UpstreamCartBenchmarkSetup"/>; a consumer can
    /// return its own cart setup so the conversion runs over its real cart graph.
    /// </summary>
    ICartModuleBenchmarkSetup CreateCartSetup();

    /// <summary>
    /// Contributes the module's order registrations to the benchmark DI container built by
    /// <see cref="OrderBenchmarkHost"/>: the order builder (<c>ICustomerOrderBuilder</c>), the order
    /// aggregate + repository (<c>ICustomerOrderAggregateRepository</c>), and any command/handler
    /// overrides (<c>OverrideCommandType().WithCommandHandler()</c>). Called AFTER the host registers
    /// the base XOrder handler and the shared mocked I/O leaves, so registrations here win by DI
    /// last-registration semantics. Upstream contributes nothing (the host's base wiring is the
    /// measured subject).
    /// </summary>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Builds the command the benchmark dispatches. Its <b>runtime type</b> drives MediatR to the right
    /// handler — upstream returns a <see cref="CreateOrderFromCartCommand"/> (base handler); a consumer
    /// that registered <c>OverrideCommandType&lt;CreateOrderFromCartCommand, TOverride&gt;()</c> returns
    /// its <c>TOverride</c> so its handler runs. <paramref name="cartId"/> is the benchmark cart id.
    /// </summary>
    CreateOrderFromCartCommand CreateCommand(string cartId);
}
