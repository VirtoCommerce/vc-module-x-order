namespace VirtoCommerce.XOrder.Benchmark;

/// <summary>
/// The product-type mix the order-creation operation is exercised against. Configured items carry
/// a <c>ConfigurationItem</c> set that <c>ConvertCartToOrder</c> maps across the cart→order
/// boundary and the recalculates walk — a distinct path from flat-SKU items.
/// </summary>
public enum CartShape
{
    /// <summary>Plain line items — baseline shape.</summary>
    Flat,

    /// <summary>Configured — each line item carries a configuration-item set.</summary>
    Configured,
}
