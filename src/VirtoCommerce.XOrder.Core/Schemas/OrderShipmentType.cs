using AutoMapper;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XOrder.Core.Extensions;
using OrderSettings = VirtoCommerce.OrdersModule.Core.ModuleConstants.Settings.General;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class OrderShipmentType : ExtendableGraphType<Shipment>
    {
        public OrderShipmentType(
            IMapper mapper,
            IMemberService memberService,
            IDataLoaderContextAccessor dataLoader,
            IDynamicPropertyResolverService dynamicPropertyResolverService,
            ILocalizableSettingService localizableSettingService)
        {
            Field(x => x.Id, nullable: false);
            Field(x => x.OperationType, nullable: false);
            Field(x => x.ParentOperationId, nullable: true);
            Field(x => x.Number, nullable: false);
            Field(x => x.IsApproved, nullable: false);
            LocalizedField(x => x.Status, OrderSettings.ShipmentStatus, localizableSettingService, nullable: true);
            Field(x => x.Comment, nullable: true);
            Field(x => x.OuterId, nullable: true);
            Field(x => x.IsCancelled, nullable: false);
            Field(x => x.CancelledDate, nullable: true);
            Field(x => x.CancelReason, nullable: true);
            Field(x => x.ObjectType, nullable: false);

            Field(x => x.OrganizationId, nullable: true);
            Field(x => x.OrganizationName, nullable: true);
            Field(x => x.FulfillmentCenterId, nullable: true);
            Field(x => x.FulfillmentCenterName, nullable: true);
            Field(x => x.EmployeeId, nullable: true);
            Field(x => x.EmployeeName, nullable: true);
            Field(x => x.ShipmentMethodCode, nullable: true);
            Field(x => x.ShipmentMethodOption, nullable: true);
            Field<OrderShippingMethodType>(nameof(Shipment.ShippingMethod).ToCamelCase()).Resolve(x => x.Source.ShippingMethod);
            Field(x => x.CustomerOrderId, nullable: true);
            Field(x => x.WeightUnit, nullable: true);
            Field(x => x.Weight, nullable: true);
            Field(x => x.MeasureUnit, nullable: true);
            Field(x => x.Height, nullable: true);
            Field(x => x.Length, nullable: true);
            Field(x => x.Width, nullable: true);
            ExtendableField<OrderAddressType>(nameof(Shipment.DeliveryAddress).ToCamelCase(), resolve: x => x.Source.DeliveryAddress);

            Field(x => x.TaxType, nullable: true);
            Field(x => x.TaxPercentRate, nullable: false);

            Field(x => x.TrackingNumber, nullable: true);
            Field(x => x.TrackingUrl, nullable: true);
            Field(x => x.DeliveryDate, nullable: true);

            Field<NonNullGraphType<MoneyType>>(nameof(Shipment.Price).ToCamelCase())
                .Resolve(context => new Money(context.Source.Price, context.GetOrderCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(Shipment.PriceWithTax).ToCamelCase())
                .Resolve(context => new Money(context.Source.PriceWithTax, context.GetOrderCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(Shipment.Fee).ToCamelCase())
                .Resolve(context => new Money(context.Source.Fee, context.GetOrderCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(Shipment.FeeWithTax).ToCamelCase())
                .Resolve(context => new Money(context.Source.FeeWithTax, context.GetOrderCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(Shipment.Total).ToCamelCase())
                .Resolve(context => new Money(context.Source.Total, context.GetOrderCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(Shipment.TotalWithTax).ToCamelCase())
                .Resolve(context => new Money(context.Source.TotalWithTax, context.GetOrderCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(Shipment.DiscountAmount).ToCamelCase())
                .Resolve(context => new Money(context.Source.DiscountAmount, context.GetOrderCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(Shipment.DiscountAmountWithTax).ToCamelCase())
                .Resolve(context => new Money(context.Source.DiscountAmountWithTax, context.GetOrderCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(Shipment.TaxTotal).ToCamelCase())
                .Resolve(context => new Money(context.Source.TaxTotal, context.GetOrderCurrency()));
            Field<NonNullGraphType<CurrencyType>>(nameof(Shipment.Currency).ToCamelCase())
                .Resolve(context => context.GetOrderCurrency());

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<OrderTaxDetailType>>>>(nameof(Shipment.TaxDetails)).Resolve(x => x.Source.TaxDetails);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<OrderShipmentItemType>>>>(nameof(Shipment.Items)).Resolve(x => x.Source.Items);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<OrderShipmentPackageType>>>>(nameof(Shipment.Packages)).Resolve(x => x.Source.Packages);
            ExtendableField<NonNullGraphType<ListGraphType<NonNullGraphType<PaymentInType>>>>(nameof(Shipment.InPayments), resolve: x => x.Source.InPayments);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<OrderDiscountType>>>>(nameof(Shipment.Discounts)).Resolve(x => x.Source.Discounts);

            var vendorField = new FieldType
            {
                Name = "vendor",
                Type = GraphTypeExtensionHelper.GetActualType<VendorType>(),
                Resolver = new FuncFieldResolver<Shipment, IDataLoaderResult<ExpVendor>>(context =>
                {
                    return dataLoader.LoadVendor(memberService, mapper, loaderKey: "order_vendor", vendorId: context.Source.VendorId);
                })
            };
            AddField(vendorField);

            ExtendableFieldAsync<NonNullGraphType<ListGraphType<NonNullGraphType<DynamicPropertyValueType>>>>(
                "dynamicProperties",
                "Customer order Shipment dynamic property values",
                QueryArgumentPresets.GetArgumentForDynamicProperties(),
                async context => await dynamicPropertyResolverService.LoadDynamicPropertyValues(context.Source, context.GetCultureName()));
        }
    }
}
