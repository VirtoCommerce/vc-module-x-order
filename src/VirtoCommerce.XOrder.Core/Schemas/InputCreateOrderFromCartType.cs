using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class InputCreateOrderFromCartType : ExtendableInputGraphType
    {
        public InputCreateOrderFromCartType()
        {
            Field<StringGraphType>("cartId",
                "Cart ID");
        }
    }
}
