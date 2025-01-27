using GraphQL.Types;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XOrder.Core.Extensions;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class OrderDiscountType : ExtendableGraphType<Discount>
    {
        public OrderDiscountType()
        {
            Field<NonNullGraphType<MoneyType>>("Amount")
                .Description("Order discount amount")
                .Resolve(context => new Money(context.Source.DiscountAmount, context.GetOrderCurrency()));
            Field(x => x.Coupon, nullable: true);
            Field(x => x.PromotionId, nullable: true);
            Field<StringGraphType>("PromotionName").Description("Name of the promotion").Resolve(context => context.Source.Name);
            Field<StringGraphType>("PromotionDescription").Description("Description of the promotion").Resolve(context => context.Source.Description);

            // Deprecated
            Field<StringGraphType>("Description").Resolve(context => context.Source.Description)
                .DeprecationReason("Use the new PromotionDescription field instead");
        }
    }
}
