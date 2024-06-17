using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class OrganizationOrderQueryConnectionArguments : ArgumentList
    {
        public OrganizationOrderQueryConnectionArguments()
        {
            Argument<StringGraphType>("filter", "This parameter applies a filter to the query results");
            Argument<StringGraphType>("sort", "The sort expression");
            Argument<StringGraphType>("cultureName", "Culture name (\"en-US\")");
            Argument<StringGraphType>("organizationId", "");
        }

        public virtual OrganizationOrderQueryConnectionArguments AddArguments(QueryArguments arguments)
        {
            foreach (var argument in arguments)
            {
                Add(argument);
            }

            return this;
        }
    }
}
