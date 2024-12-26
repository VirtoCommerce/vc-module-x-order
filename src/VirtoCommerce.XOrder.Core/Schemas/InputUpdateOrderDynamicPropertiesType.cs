using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class InputUpdateOrderDynamicPropertiesType : ExtendableInputGraphType
    {
        public InputUpdateOrderDynamicPropertiesType()
        {
            Field<StringGraphType>("orderId");
            Field<NonNullGraphType<ListGraphType<InputDynamicPropertyValueType>>>("dynamicProperties");
        }
    }
}
