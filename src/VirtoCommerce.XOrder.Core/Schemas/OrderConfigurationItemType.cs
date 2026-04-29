using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using MediatR;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Schemas;
using VirtoCommerce.XOrder.Core.Extensions;

namespace VirtoCommerce.XOrder.Core.Schemas;

public class OrderConfigurationItemType : ExtendableGraphType<ConfigurationItem>
{
    public OrderConfigurationItemType(
        IMediator mediator,
        IDataLoaderContextAccessor dataLoader)
    {
        Field(x => x.Id, nullable: false).Description("Configuration item ID");
        Field(x => x.SectionId, nullable: false).Description("Configuration item section ID");
        Field(x => x.Name, nullable: true).Description("Configuration item name");
        Field(x => x.ProductId, nullable: true).Description("Configuration item product ID");
        Field(x => x.Sku, nullable: true).Description("Configuration item SKU");
        Field(x => x.ImageUrl, nullable: true).Description("Configuration item image URL");
        Field(x => x.Quantity, nullable: true).Description("Configuration item product quantity");
        Field(x => x.Type, nullable: false).Description("Configuration item type. Possible values: 'Product', 'Variation', 'Text', 'File'");
        Field(x => x.CustomText, nullable: true).Description("Custom text for 'Text' configuration item section");

        Field<NonNullGraphType<MoneyType>>(nameof(ConfigurationItem.Price))
            .Description("List price")
            .Resolve(context => context.Source.Price.ToMoney(context.GetOrderCurrency()));

        Field<NonNullGraphType<MoneyType>>(nameof(ConfigurationItem.SalePrice))
            .Description("Sale price")
            .Resolve(context => context.Source.SalePrice.ToMoney(context.GetOrderCurrency()));

        Field<NonNullGraphType<MoneyType>>(nameof(ConfigurationItem.ExtendedPrice))
            .Description("Extended price")
            .Resolve(context => context.Source.ExtendedPrice.ToMoney(context.GetOrderCurrency()));

        ExtendableField<ListGraphType<OrderConfigurationItemFileType>>(nameof(ConfigurationItem.Files),
            resolve: context => context.Source.Files,
            description: "List of files for 'File' configuration item section");

        var productField = new FieldType
        {
            Name = "product",
            Type = GraphTypeExtensionHelper.GetActualType<ProductType>(),
            Resolver = new FuncFieldResolver<ConfigurationItem, IDataLoaderResult<ExpProduct>>(context =>
                dataLoader.LoadOrderProductWithSnapshot(
                    context, $"order_configurationItems_products_{context.Source.CustomerOrderId}", context.Source.ProductId)),
        };
        AddField(productField);
    }
}
