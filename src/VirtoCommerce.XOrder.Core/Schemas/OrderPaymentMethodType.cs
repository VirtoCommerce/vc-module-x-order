using GraphQL;
using GraphQL.Types;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XOrder.Core.Extensions;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class OrderPaymentMethodType : ExtendableGraphType<PaymentMethod>
    {
        public OrderPaymentMethodType()
        {
            Field(x => x.Code, nullable: false);
            Field(x => x.Description, nullable: true);
            Field(x => x.LogoUrl, nullable: true);
            Field(x => x.Priority, nullable: false);
            Field(x => x.IsActive, nullable: false);
            Field(x => x.IsAvailableForPartial, nullable: false);
            Field(x => x.TypeName, nullable: false);
            Field(x => x.StoreId, nullable: true);

            Field<StringGraphType>("name")
                .Resolve(context => GetLocalizedValue(context, context.Source.LocalizedName, context.Source.Id, context.Source.Name))
                .Description("Localized name of payment method.");

            Field<NonNullGraphType<CurrencyType>>(nameof(PaymentMethod.Currency).ToCamelCase())
                .Resolve(context => context.GetOrderCurrency());

            Field<NonNullGraphType<MoneyType>>(nameof(PaymentMethod.Price).ToCamelCase())
                .Resolve(context => new Money(context.Source.Price, context.GetOrderCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(PaymentMethod.PriceWithTax).ToCamelCase())
                .Resolve(context => new Money(context.Source.PriceWithTax, context.GetOrderCurrency()));

            Field<NonNullGraphType<MoneyType>>(nameof(PaymentMethod.DiscountAmount).ToCamelCase())
                .Resolve(context => new Money(context.Source.DiscountAmount, context.GetOrderCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(PaymentMethod.DiscountAmountWithTax).ToCamelCase())
                .Resolve(context => new Money(context.Source.DiscountAmountWithTax, context.GetOrderCurrency()));

            Field<NonNullGraphType<MoneyType>>(nameof(PaymentMethod.Total).ToCamelCase())
                .Resolve(context => new Money(context.Source.Total, context.GetOrderCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(PaymentMethod.TotalWithTax).ToCamelCase())
                .Resolve(context => new Money(context.Source.TotalWithTax, context.GetOrderCurrency()));

            Field(x => x.TaxType, nullable: true);
            Field(x => x.TaxPercentRate, nullable: false);
            Field<NonNullGraphType<MoneyType>>(nameof(PaymentMethod.TaxTotal).ToCamelCase())
                .Resolve(context => new Money(context.Source.TaxTotal, context.GetOrderCurrency()));
            Field<ListGraphType<NonNullGraphType<OrderTaxDetailType>>>(nameof(PaymentMethod.TaxDetails))
                .Resolve(x => x.Source.TaxDetails);

            Field<NonNullGraphType<IntGraphType>>(nameof(PaymentMethod.PaymentMethodType))
                .Resolve(context => (int)context.Source.PaymentMethodType);
            Field<NonNullGraphType<IntGraphType>>(nameof(PaymentMethod.PaymentMethodGroupType))
                .Resolve(context => (int)context.Source.PaymentMethodGroupType);
        }

        private static string GetLocalizedValue(IResolveFieldContext context, LocalizedString localizedString, string paymentMethodId, string fallbackValue = null)
        {
            var cultureName = context.GetArgumentOrValue<string>("cultureName");

            if (string.IsNullOrEmpty(cultureName))
            {
                cultureName = context.GetArgumentOrValue<CustomerOrderAggregate>(paymentMethodId)?.Order?.LanguageCode;
            }

            if (!string.IsNullOrEmpty(cultureName))
            {
                var localizedValue = localizedString?.GetValue(cultureName);
                if (!string.IsNullOrEmpty(localizedValue))
                {
                    return localizedValue;
                }
            }

            return fallbackValue;
        }
    }
}
