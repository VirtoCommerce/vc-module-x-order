using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XOrder.Core.Schemas;

public class OrderConfigurationItemFileType : ExtendableGraphType<ConfigurationItemFile>
{
    public OrderConfigurationItemFileType()
    {
        Field(x => x.Url, nullable: false).Description("URL of the file");
        Field(x => x.Name, nullable: false).Description("Name of the file");
        Field(x => x.Size, nullable: false).Description("Size of the file");
        Field(x => x.ContentType, nullable: true).Description("MIME type of the file");
    }
}
