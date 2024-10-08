using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class InputUpdateOrderItemDynamicPropertiesType : InputObjectGraphType
    {
        public InputUpdateOrderItemDynamicPropertiesType()
        {
            Field<StringGraphType>("orderId");
            Field<StringGraphType>("lineItemId");
            Field<NonNullGraphType<ListGraphType<InputDynamicPropertyValueType>>>("dynamicProperties");
        }
    }
}
