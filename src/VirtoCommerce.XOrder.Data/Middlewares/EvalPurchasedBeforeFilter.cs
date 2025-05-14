using System;
using System.Linq;
using System.Threading.Tasks;
using PipelineNet.Middleware;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.XCatalog.Data.Index;
using static VirtoCommerce.OrdersModule.Core.ModuleConstants;

namespace VirtoCommerce.XOrder.Data.Middlewares;

public class EvalPurchasedBeforeFilter : IAsyncMiddleware<IndexSearchRequestBuilder>
{
    public async Task Run(IndexSearchRequestBuilder parameter, Func<IndexSearchRequestBuilder, Task> next)
    {
        if (!string.IsNullOrEmpty(parameter.UserId) && !string.IsNullOrEmpty(parameter.StoreId) &&
           parameter.Filter is AndFilter andFilter && !andFilter.ChildFilters.IsNullOrEmpty())
        {
            var purchasedBeforeFilter = andFilter.ChildFilters.OfType<TermFilter>().FirstOrDefault(x => x.FieldName == "isPurchased");
            if (purchasedBeforeFilter != null)
            {
                // remove this special filter
                andFilter.ChildFilters.Remove(purchasedBeforeFilter);

                // add new filter
                var filterName = $"{PurchasedProductDocumentPrefix}_{parameter.StoreId}";
                parameter.AddTerms(new[] { $"{filterName}:{parameter.UserId}" });

                UpdateAggregations(parameter, andFilter, filterName);
            }
        }

        await next(parameter);
    }

    private static void UpdateAggregations(IndexSearchRequestBuilder parameter, AndFilter andFilter, string filterName)
    {
        foreach (var aggregation in parameter.Aggregations)
        {
            if (aggregation.Filter is not AndFilter aggregationFilter)
            {
                continue;
            }

            var purchasedBeforeAggregationFilter = aggregationFilter.ChildFilters.OfType<TermFilter>().FirstOrDefault(x => x.FieldName == "isPurchased");
            if (purchasedBeforeAggregationFilter != null)
            {
                aggregationFilter.ChildFilters.Remove(purchasedBeforeAggregationFilter);

                purchasedBeforeAggregationFilter = andFilter.ChildFilters.OfType<TermFilter>().FirstOrDefault(x => x.FieldName == filterName);
                if (purchasedBeforeAggregationFilter != null)
                {
                    var clonedFitler = purchasedBeforeAggregationFilter.CloneTyped();
                    aggregationFilter.ChildFilters.Add(clonedFitler);
                }
            }
        }
    }
}
