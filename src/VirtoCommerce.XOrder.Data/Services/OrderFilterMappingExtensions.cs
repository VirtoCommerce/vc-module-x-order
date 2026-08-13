using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.OrdersModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.Xapi.Core.Index;

namespace VirtoCommerce.XOrder.Data.Services;

public static class OrderFilterMappingExtensions
{
    public static void MapTo(this IList<IFilter> filters, PaymentSearchCriteria criteria)
    {
        foreach (var term in filters.OfType<TermFilter>())
        {
            term.MapTo(criteria);
        }
    }
}
