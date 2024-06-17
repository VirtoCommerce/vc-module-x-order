using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class OrderQueryArguments : ArgumentList
    {
        public OrderQueryArguments()
        {
            Argument<StringGraphType>("id");
            Argument<StringGraphType>("number");
            Argument<StringGraphType>("cultureName", "Culture name (\"en-US\")");
        }
    }
}
