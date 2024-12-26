using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class InputUpdateOrderPaymentDynamicPropertiesType : ExtendableInputGraphType
    {
        public InputUpdateOrderPaymentDynamicPropertiesType()
        {
            Field<StringGraphType>("orderId");
            Field<StringGraphType>("paymentId");
            Field<NonNullGraphType<ListGraphType<InputDynamicPropertyValueType>>>("dynamicProperties");
        }
    }
}
