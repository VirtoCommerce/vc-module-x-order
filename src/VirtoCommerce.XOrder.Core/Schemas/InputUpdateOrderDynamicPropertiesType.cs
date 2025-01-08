using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class InputUpdateOrderDynamicPropertiesType : InputObjectGraphType
    {
        public InputUpdateOrderDynamicPropertiesType()
        {
            Field<StringGraphType>("orderId");
            Field<NonNullGraphType<ListGraphType<InputDynamicPropertyValueType>>>("dynamicProperties");
        }
    }
}
