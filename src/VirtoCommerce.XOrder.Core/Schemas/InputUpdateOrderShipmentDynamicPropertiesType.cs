using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class InputUpdateOrderShipmentDynamicPropertiesType : ExtendableInputGraphType
    {
        public InputUpdateOrderShipmentDynamicPropertiesType()
        {
            Field<StringGraphType>("orderId");
            Field<StringGraphType>("shipmentId");
            Field<NonNullGraphType<ListGraphType<InputDynamicPropertyValueType>>>("dynamicProperties");
        }
    }
}
