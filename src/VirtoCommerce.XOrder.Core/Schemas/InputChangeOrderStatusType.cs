using GraphQL.Types;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class InputChangeOrderStatusType : InputObjectGraphType
    {
        public InputChangeOrderStatusType()
        {
            Field<NonNullGraphType<StringGraphType>>("orderId")
                .Description("Order ID");
            Field<NonNullGraphType<StringGraphType>>("status")
                .Description("Order status");
        }
    }
}
