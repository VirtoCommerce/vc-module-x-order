using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class InputChangeOrderStatusType : ExtendableInputGraphType
    {
        public InputChangeOrderStatusType()
        {
            Field<NonNullGraphType<StringGraphType>>("orderId",
                "Order ID");
            Field<NonNullGraphType<StringGraphType>>("status",
                "Order status");
        }
    }
}
