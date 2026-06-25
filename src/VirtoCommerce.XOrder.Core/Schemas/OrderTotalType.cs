using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XOrder.Core.Models;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class OrderTotalType : ExtendableGraphType<OrderTotalAggregate>
    {
        public OrderTotalType()
        {
            Field(x => x.IsDefaultTotalCurrency, nullable: false).Description("Is current total in default total currency");

            Field<NonNullGraphType<MoneyType>>("total")
                .Description("Cart total")
                .Resolve(context => context.Source.OrderTotal.Total.ToMoney(context.Source.Currency));

            Field<NonNullGraphType<MoneyType>>("subTotal")
                .Description("Cart subtotal")
                .Resolve(context => context.Source.OrderTotal.SubTotal.ToMoney(context.Source.Currency));

            Field<NonNullGraphType<MoneyType>>("taxTotal")
                .Description("Total tax")
                .Resolve(context => context.Source.OrderTotal.TaxTotal.ToMoney(context.Source.Currency));

            Field<NonNullGraphType<MoneyType>>("discountTotal")
                .Description("Total discount")
                .Resolve(context => context.Source.OrderTotal.DiscountTotal.ToMoney(context.Source.Currency));
        }
    }
}
