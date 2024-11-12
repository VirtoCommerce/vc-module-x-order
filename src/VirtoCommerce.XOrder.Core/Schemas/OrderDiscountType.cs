using GraphQL.Types;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XOrder.Core.Extensions;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class OrderDiscountType : ObjectGraphType<Discount>
    {
        public OrderDiscountType()
        {
            Field<NonNullGraphType<MoneyType>>("Amount",
                "Order discount amount",
                resolve: context => new Money(context.Source.DiscountAmount, context.GetOrderCurrency()));
            Field(x => x.Coupon, nullable: true);
            Field<StringGraphType>("Description", resolve: context => context.Source.Description, deprecationReason: "Use the new PromotionDescription field instead");
            Field<StringGraphType>("PromotionDescription", "Description of the promotion", resolve: context => context.Source.Description);
            Field(x => x.PromotionId, nullable: true);
            Field<StringGraphType>("PromotionName", "Name of the promotion", resolve: context => context.Source.Name);
        }
    }
}
