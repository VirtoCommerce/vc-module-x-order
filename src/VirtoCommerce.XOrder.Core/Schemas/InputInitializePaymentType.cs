using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class InputInitializePaymentType : ExtendableInputGraphType
    {
        public InputInitializePaymentType()
        {
            Field<StringGraphType>("orderId", "Order Id");
            Field<NonNullGraphType<StringGraphType>>("paymentId", "Payment Id");
        }
    }
}
