using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class InputAuthorizePaymentType : InputObjectGraphType
    {
        public InputAuthorizePaymentType()
        {
            Field<StringGraphType>("orderId").Description("Order Id");
            Field<NonNullGraphType<StringGraphType>>("paymentId").Description("Payment Id");
            Field<ListGraphType<InputKeyValueType>>("parameters").Description("Input parameters");
        }
    }
}
