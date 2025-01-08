using GraphQL.Types;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class InputProcessOrderPaymentType : InputObjectGraphType
    {
        public InputProcessOrderPaymentType()
        {
            Field<NonNullGraphType<StringGraphType>>("orderId")
                .Description("Order ID");
            Field<NonNullGraphType<StringGraphType>>("paymentId")
                .Description("Payment ID");
            Field<InputOrderBankCardInfoType>("bankCardInfo")
                .Description("Credit card details");
        }
    }
}
