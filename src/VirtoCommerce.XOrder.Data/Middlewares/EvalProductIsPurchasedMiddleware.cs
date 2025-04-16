using System;
using System.Linq;
using System.Threading.Tasks;
using PipelineNet.Middleware;
using VirtoCommerce.OrdersModule.Core.Models;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Data.Index;

namespace VirtoCommerce.XOrder.Data.Middlewares;

public class EvalProductIsPurchasedMiddleware : IAsyncMiddleware<SearchProductResponse>
{
    private readonly IPurchasedProductService _purchasedProductService;
    private readonly IPurchasedProductSearchService _purchasedProductSearchService;

    public EvalProductIsPurchasedMiddleware(IPurchasedProductService purchasedProductService, IPurchasedProductSearchService purchasedProductSearchService)
    {
        _purchasedProductService = purchasedProductService;
        _purchasedProductSearchService = purchasedProductSearchService;
    }

    public async Task Run(SearchProductResponse parameter, Func<SearchProductResponse, Task> next)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        var query = parameter.Query ?? throw new OperationCanceledException("Query must be set");

        var productIds = parameter.Results.Select(x => x.Id).ToArray();
        var responseGroup = EnumUtility.SafeParse(query.GetResponseGroup(), ExpProductResponseGroup.None);

        if (responseGroup.HasFlag(ExpProductResponseGroup.LoadPurchasedBefore) && productIds.Length != 0)
        {
            var critieria = AbstractTypeFactory<PurchasedProductSearchCriteria>.TryCreateInstance();
            critieria.UserId = query.UserId;
            critieria.ProductIds = productIds;
            critieria.Take = productIds.Length;

            var purchasedProductsIds = await _purchasedProductSearchService.SearchAsync(critieria);

            if (purchasedProductsIds.Results.Count > 0)
            {
                parameter.Results.Apply((item) =>
                {
                    item.IsPurchasedBefore = purchasedProductsIds.Results.Any(x => x.ProductId == item.Id);
                });
            }
        }

        await next(parameter);
    }
}

public class EvalPurchasedBeforeFilter : IAsyncMiddleware<IndexSearchRequestBuilder>
{
    private readonly IPurchasedProductService _purchasedProductService;
    private readonly IPurchasedProductSearchService _purchasedProductSearchService;

    public EvalPurchasedBeforeFilter(IPurchasedProductService purchasedProductService, IPurchasedProductSearchService purchasedProductSearchService)
    {
        _purchasedProductService = purchasedProductService;
        _purchasedProductSearchService = purchasedProductSearchService;
    }

    public async Task Run(IndexSearchRequestBuilder parameter, Func<IndexSearchRequestBuilder, Task> next)
    {
        if (parameter.Filter is SearchModule.Core.Model.AndFilter andFilter && !andFilter.ChildFilters.IsNullOrEmpty())
        {
            var purchasedBeforeFilter = andFilter.ChildFilters.OfType<TermFilter>().FirstOrDefault(x => x.FieldName == "purchasedBefore");
            if (purchasedBeforeFilter != null)
            {
                // remove special filter
                andFilter.ChildFilters.Remove(purchasedBeforeFilter);

                // remove the old id filter
                var oldObjectIdsFilters = andFilter.ChildFilters.OfType<IdsFilter>().ToList();
                foreach (var idFilter in oldObjectIdsFilters)
                {
                    andFilter.ChildFilters.Remove(idFilter);
                }

                // get feed convert to id filter and add it
                var critieria = AbstractTypeFactory<PurchasedProductSearchCriteria>.TryCreateInstance();
                critieria.UserId = parameter.UserId;
                critieria.Skip = parameter.SkipBackup;
                critieria.Take = parameter.Take;

                var purchasedProductsIds = await _purchasedProductSearchService.SearchAsync(critieria);

                parameter.Skip = parameter.SkipBackup;
                parameter.Take = parameter.TakeBackup;
                parameter.AddObjectIds(purchasedProductsIds.Results.Select(x => x.ProductId));

                // sorting???
            }
        }

        await next(parameter);
    }
}
