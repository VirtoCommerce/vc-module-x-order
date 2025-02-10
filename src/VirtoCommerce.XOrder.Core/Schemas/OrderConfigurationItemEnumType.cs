using GraphQL.Types;
using VirtoCommerce.OrdersModule.Core.Model;

namespace VirtoCommerce.XOrder.Core.Schemas;

public class OrderConfigurationItemEnumType : EnumerationGraphType<ConfigurationItemType>
{
    public OrderConfigurationItemEnumType()
    {
        Name = "OrderConfigurationItemEnumType";
    }
}
