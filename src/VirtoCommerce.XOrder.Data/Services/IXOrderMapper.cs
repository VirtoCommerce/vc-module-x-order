using VirtoCommerce.OrdersModule.Core.Model.Search;
using VirtoCommerce.Xapi.Core.Models.Facets;

namespace VirtoCommerce.XOrder.Data.Services;

public interface IXOrderMapper
{
    FacetResult ToFacetResult(OrderAggregation source, string cultureName);
}
