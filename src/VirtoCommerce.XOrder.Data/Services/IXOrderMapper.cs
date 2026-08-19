using System.Collections.Generic;
using VirtoCommerce.OrdersModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.Xapi.Core.Models.Facets;

namespace VirtoCommerce.XOrder.Data.Services;

public interface IXOrderMapper
{
    FacetResult ToFacetResult(OrderAggregation source, FacetMappingContext context);

    void MapTo(IList<IFilter> filters, PaymentSearchCriteria criteria);
}
