using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using GraphQL.Types.Relay;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Models.Facets;
using CoreFacets = VirtoCommerce.Xapi.Core.Schemas.Facets;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class CustomerOrderConnectionType<TNodeType> : ConnectionType<TNodeType>
        where TNodeType : IGraphType
    {
        public CustomerOrderConnectionType()
        {
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<CoreFacets.TermFacetResultType>>>>("term_facets").Description("Term facets")
                .Resolve(context => ((CustomerOrderConnection<CustomerOrderAggregate>)context.Source).Facets.OfType<TermFacetResult>());

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<CoreFacets.RangeFacetResultType>>>>("range_facets").Description("Range facets")
                .Resolve(context => ((CustomerOrderConnection<CustomerOrderAggregate>)context.Source).Facets.OfType<RangeFacetResult>());

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<CoreFacets.FilterFacetResultType>>>>("filter_facets").Description("Filter facets")
                .Resolve(context => ((CustomerOrderConnection<CustomerOrderAggregate>)context.Source).Facets.OfType<FilterFacetResult>());
        }
    }

    public class CustomerOrderConnection<TNode> : PagedConnection<TNode>
    {
        public CustomerOrderConnection(IEnumerable<TNode> superset, int skip, int take, int totalCount)
            : base(superset, skip, take, totalCount)
        {
        }

        public IList<FacetResult> Facets { get; set; }
    }
}
