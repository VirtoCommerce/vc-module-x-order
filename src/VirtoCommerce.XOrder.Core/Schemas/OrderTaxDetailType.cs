using GraphQL;
using GraphQL.Types;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CoreModule.Core.Tax;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XOrder.Core.Extensions;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class OrderTaxDetailType : ExtendableGraphType<TaxDetail>
    {
        public OrderTaxDetailType()
        {
            Field<NonNullGraphType<MoneyType>>(nameof(TaxDetail.Rate).ToCamelCase()).Resolve(context => new Money(context.Source.Rate, context.GetOrderCurrency()));
            Field<NonNullGraphType<MoneyType>>(nameof(TaxDetail.Amount).ToCamelCase()).Resolve(context => new Money(context.Source.Amount, context.GetOrderCurrency()));
            Field(x => x.Name);
        }
    }
}
