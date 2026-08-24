using System;
using AutoMapper;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Schemas;
using VirtoCommerce.XOrder.Core.Extensions;
using OrderSettings = VirtoCommerce.OrdersModule.Core.ModuleConstants.Settings.General;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class OrderLineItemType : ExtendableGraphType<LineItem>
    {
        public OrderLineItemType(
            IDataLoaderContextAccessor dataLoader,
            IDynamicPropertyResolverService dynamicPropertyResolverService,
            IMapper mapper,
            IMemberService memberService,
            ILocalizableSettingService localizableSettingService)
        {
            Field(x => x.Id, nullable: false);
            Field(x => x.ProductType, nullable: true);
            Field(x => x.Name, nullable: false);
            Field(x => x.Comment, nullable: true);
            Field(x => x.ImageUrl, nullable: true);
            Field(x => x.IsGift, nullable: true);
            Field(x => x.ShippingMethodCode, nullable: true);
            Field(x => x.FulfillmentLocationCode, nullable: true);
            Field(x => x.FulfillmentCenterId, nullable: true);
            Field(x => x.FulfillmentCenterName, nullable: true);
            Field(x => x.OuterId, nullable: true);
            Field(x => x.ProductOuterId, nullable: true);
            Field(x => x.WeightUnit, nullable: true);
            Field(x => x.Weight, nullable: true);
            Field(x => x.MeasureUnit, nullable: true);
            Field(x => x.Height, nullable: true);
            Field(x => x.Length, nullable: true);
            Field(x => x.Width, nullable: true);
            Field(x => x.IsCancelled, nullable: false);
            Field(x => x.CancelledDate, nullable: true);
            Field(x => x.CancelReason, nullable: true);
            Field(x => x.ObjectType, nullable: false);
            LocalizedField(x => x.Status, OrderSettings.OrderLineItemStatuses, localizableSettingService, nullable: true);

            Field(x => x.CategoryId, nullable: true);
            Field(x => x.CatalogId, nullable: false);

            Field(x => x.Sku, nullable: false);
            Field(x => x.PriceId, nullable: true);
            Field<NonNullGraphType<MoneyType>>(nameof(LineItem.Price).ToCamelCase())
                .Resolve(context => context.Source.Price.ToMoney(context.GetOrderItemCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(LineItem.PriceWithTax).ToCamelCase())
                .Resolve(context => context.Source.PriceWithTax.ToMoney(context.GetOrderItemCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(LineItem.ListTotal).ToCamelCase())
                .Resolve(context => context.Source.ListTotal.ToMoney(context.GetOrderItemCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(LineItem.ListTotalWithTax).ToCamelCase())
                .Resolve(context => context.Source.ListTotalWithTax.ToMoney(context.GetOrderItemCurrency()));
            Field(x => x.TaxType, nullable: true);
            Field(x => x.TaxPercentRate, nullable: false);
            Field(x => x.ReserveQuantity, nullable: false);
            Field(x => x.Quantity, nullable: false);
            Field(x => x.ProductId, nullable: false);

            Field<NonNullGraphType<CurrencyType>>(nameof(LineItem.Currency).ToCamelCase())
                .Resolve(context => context.GetOrderItemCurrency());
            Field<NonNullGraphType<MoneyType>>(nameof(LineItem.DiscountAmount).ToCamelCase())
                .Resolve(context => context.Source.DiscountAmount.ToMoney(context.GetOrderItemCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(LineItem.DiscountAmountWithTax).ToCamelCase())
                .Resolve(context => context.Source.DiscountAmountWithTax.ToMoney(context.GetOrderItemCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(LineItem.DiscountTotal).ToCamelCase())
                .Resolve(context => context.Source.DiscountTotal.ToMoney(context.GetOrderItemCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(LineItem.DiscountTotalWithTax).ToCamelCase())
                .Resolve(context => context.Source.DiscountTotalWithTax.ToMoney(context.GetOrderItemCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(LineItem.ExtendedPrice).ToCamelCase())
                .Resolve(context => context.Source.ExtendedPrice.ToMoney(context.GetOrderItemCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(LineItem.ExtendedPriceWithTax).ToCamelCase())
                .Resolve(context => context.Source.ExtendedPriceWithTax.ToMoney(context.GetOrderItemCurrency()));
            Field<NonNullGraphType<BooleanGraphType>>("showPlacedPrice")
                .Description("Indicates whether the PlacedPrice should be visible to the customer")
                .Resolve(context => context.Source.IsDiscountAmountRounded);
            Field<NonNullGraphType<MoneyType>>(nameof(LineItem.PlacedPrice).ToCamelCase())
                .Resolve(context => context.Source.PlacedPrice.ToMoney(context.GetOrderItemCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(LineItem.PlacedPriceWithTax).ToCamelCase())
                .Resolve(context => context.Source.PlacedPriceWithTax.ToMoney(context.GetOrderItemCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(LineItem.TaxTotal).ToCamelCase())
                .Resolve(context => context.Source.TaxTotal.ToMoney(context.GetOrderItemCurrency()));
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<OrderTaxDetailType>>>>(nameof(LineItem.TaxDetails))
                .Resolve(x => x.Source.TaxDetails);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<OrderDiscountType>>>>(nameof(LineItem.Discounts))
                .Resolve(x => x.Source.Discounts);

            var productField = new FieldType
            {
                Name = "product",
                Type = GraphTypeExtensionHelper.GetActualType<ProductType>(),
                Resolver = new FuncFieldResolver<LineItem, IDataLoaderResult<ExpProduct>>(context =>
                    dataLoader.LoadOrderProductWithSnapshot(
                        context, $"order_lineItems_products_{context.Source.CustomerOrderId}", context.Source.ProductId)),
            };
            AddField(productField);

            var vendorField = new FieldType
            {
                Name = "vendor",
                Type = GraphTypeExtensionHelper.GetActualType<VendorType>(),
                Resolver = new FuncFieldResolver<LineItem, IDataLoaderResult<ExpVendor>>(context =>
                    dataLoader.LoadVendor(memberService, mapper, loaderKey: "order_vendor", vendorId: context.Source.VendorId)),
            };
            AddField(vendorField);

            ExtendableFieldAsync<NonNullGraphType<ListGraphType<NonNullGraphType<DynamicPropertyValueType>>>>(
                "dynamicProperties",
                "Customer order Line item dynamic property values",
                null,
                async context => await dynamicPropertyResolverService.LoadDynamicPropertyValues(context.Source, context.GetCultureName()));

            ExtendableField<ListGraphType<OrderConfigurationItemType>>(
                "configurationItems",
                "Configuration items for configurable product",
                resolve: context => context.Source.ConfigurationItems ?? []);
        }

        [Obsolete("Use the constructor without IMediator. The mediator is resolved from context.RequestServices per request.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
        public OrderLineItemType(
            IMediator mediator,
            IDataLoaderContextAccessor dataLoader,
            IDynamicPropertyResolverService dynamicPropertyResolverService,
            IMapper mapper,
            IMemberService memberService,
            ILocalizableSettingService localizableSettingService)
            : this(dataLoader, dynamicPropertyResolverService, mapper, memberService, localizableSettingService)
        {
        }
    }
}
