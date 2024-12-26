using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class InputAuthorizePaymentType : ExtendableInputGraphType
    {
        public InputAuthorizePaymentType()
        {
            Field<StringGraphType>("orderId", "Order Id");
            Field<NonNullGraphType<StringGraphType>>("paymentId", "Payment Id");
            Field<ListGraphType<InputKeyValueType>>("parameters", "Input parameters");
        }
    }
}
