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
public interface IOrderBenchmarkSetup
{
    /// <summary>
    /// The cart setup that builds the <b>input</b> cart the order is created from. The order host loads
    /// it through <see cref="CartBenchmarkHost"/>, so the cart is recalculated exactly as the cart
    /// benchmarks build it; return your own cart setup and the conversion runs over your real cart graph.
    /// </summary>
    ICartBenchmarkSetup CreateCartSetup();

    /// <summary>
    /// Contributes the module's order registrations to the benchmark DI container built by
    /// <see cref="OrderBenchmarkHost"/>. Called AFTER the host's base registrations, so an <c>Add*</c>
    /// here wins by last-registration semantics. Use <c>Add*</c>, never <c>TryAdd*</c>: the host has
    /// already registered these service types, so a <c>TryAdd*</c> no-ops and the run silently measures
    /// the stock graph.
    /// </summary>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Builds the command the benchmark dispatches. Its <b>runtime type</b> drives MediatR to the right
    /// handler: after <c>OverrideCommandType&lt;CreateOrderFromCartCommand, TOverride&gt;()</c>, return the
    /// <c>TOverride</c> or the base handler runs instead.
    /// </summary>
    CreateOrderFromCartCommand CreateCommand(string cartId);
}
