using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class InputAddOrUpdateOrderPaymentType : ExtendableInputGraphType
    {
        public InputAddOrUpdateOrderPaymentType()
        {
            Field<NonNullGraphType<StringGraphType>>("orderId", "Order ID");
            Field<NonNullGraphType<InputOrderPaymentType>>("payment", "Payment");
        }
    }
}
