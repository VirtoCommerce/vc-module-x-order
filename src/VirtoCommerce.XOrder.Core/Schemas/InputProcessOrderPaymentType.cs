using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class InputProcessOrderPaymentType : ExtendableInputGraphType
    {
        public InputProcessOrderPaymentType()
        {
            Field<NonNullGraphType<StringGraphType>>("orderId",
                "Order ID");
            Field<NonNullGraphType<StringGraphType>>("paymentId",
                "Payment ID");
            Field<InputOrderBankCardInfoType>("bankCardInfo",
                "Credit card details");
        }
    }
}
