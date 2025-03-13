using GraphQL.Types;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XOrder.Core.Schemas;

public class OrderConfigurationItemType : ExtendableGraphType<ConfigurationItem>
{
    public OrderConfigurationItemType()
    {
        Field(x => x.Id, nullable: false).Description("Configuration item ID");
        Field(x => x.Name, nullable: true).Description("Configuration item name");
        Field(x => x.Type, nullable: false).Description("Configuration item type. Possible values: 'Product', 'Text', 'File'");
        Field(x => x.CustomText, nullable: true).Description("Configuration item custom text");

        ExtendableField<ListGraphType<OrderConfigurationItemFileType>>(nameof(ConfigurationItem.Files),
            resolve: context => context.Source.Files,
            description: "List of files for 'File' configuration item section");
    }
}
