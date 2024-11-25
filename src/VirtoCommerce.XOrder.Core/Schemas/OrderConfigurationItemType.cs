using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XOrder.Core.Schemas;

public class OrderConfigurationItemType : ExtendableGraphType<ConfigurationItem>
{
    public OrderConfigurationItemType()
    {
        Field(x => x.Id, nullable: false).Description("Configuration item ID");
        Field(x => x.Name, nullable: true).Description("Configuration item name");
    }
}

