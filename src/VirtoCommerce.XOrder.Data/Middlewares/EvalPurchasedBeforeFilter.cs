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
            }
        }

        await next(parameter);
    }
}
