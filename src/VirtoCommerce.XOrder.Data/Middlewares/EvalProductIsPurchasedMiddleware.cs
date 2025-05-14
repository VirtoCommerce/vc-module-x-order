using System;
using System.Linq;
using System.Threading.Tasks;
using PipelineNet.Middleware;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.XCatalog.Core.Models;
using OrderSettings = VirtoCommerce.OrdersModule.Core.ModuleConstants.Settings.General;

namespace VirtoCommerce.XOrder.Data.Middlewares;

public class EvalProductIsPurchasedMiddleware : IAsyncMiddleware<SearchProductResponse>
{
    private readonly IPurchasedProductsService _purchasedProductsService;
    private readonly IStoreService _storeService;

    public EvalProductIsPurchasedMiddleware(IPurchasedProductsService purchasedBeforeService, IStoreService storeService)
    {
        _purchasedProductsService = purchasedBeforeService;
        _storeService = storeService;
    }

    public async Task Run(SearchProductResponse parameter, Func<SearchProductResponse, Task> next)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        var query = parameter.Query ?? throw new OperationCanceledException("Query must be set");

        var store = (parameter.Store ?? parameter.Query.Store) ?? await _storeService.GetByIdAsync(parameter.Query.StoreId);

        var productPurchasedStoreFilterEnabled = false;
        if (store != null)
        {
            productPurchasedStoreFilterEnabled = store.Settings.GetValue<bool>(OrderSettings.PurchasedProductStoreFilter);
        }

        if (!productPurchasedStoreFilterEnabled)
        {
            await next(parameter);
            return;
        }

        var productIds = parameter.Results.Select(x => x.Id).ToArray();
        var responseGroup = EnumUtility.SafeParse(query.GetResponseGroup(), ExpProductResponseGroup.None);

        if (responseGroup.HasFlag(ExpProductResponseGroup.LoadPurchased) && productIds.Length != 0)
        {
            var critieria = AbstractTypeFactory<PurchasedProductsRequest>.TryCreateInstance();
            critieria.UserId = query.UserId;
            critieria.StoreId = query.StoreId;
            critieria.ProductIds = productIds;

            var purchasedProductsResult = await _purchasedProductsService.GetPurchasedProductsAsync(critieria);

            if (purchasedProductsResult.ProductIds.Count > 0)
            {
                parameter.Results.Apply((item) =>
                {
                    item.IsPurchased = purchasedProductsResult.ProductIds.Any(x => x == item.Id);
                });
            }
        }

        await next(parameter);
    }
}
