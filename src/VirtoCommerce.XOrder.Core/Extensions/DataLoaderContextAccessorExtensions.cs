using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.DataLoader;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Extensions;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.ProductSnapshot.Core.Services;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Queries;

namespace VirtoCommerce.XOrder.Core.Extensions;

public static class DataLoaderContextAccessorExtensions
{
    private static readonly DataLoaderResult<ExpProduct> _defaultProductResult = new((ExpProduct)null);

    public static IDataLoaderResult<ExpProduct> LoadOrderProduct(
        this IDataLoaderContextAccessor dataLoader,
        IResolveFieldContext context,
        IMediator mediator,
        string loaderKey,
        string productId)
    {
        if (string.IsNullOrEmpty(productId))
        {
            return _defaultProductResult;
        }

        var loader = dataLoader.GetOrderProductDataLoader(context, mediator, loaderKey);

        return loader.LoadAsync(productId);
    }

    public static IDataLoader<string, ExpProduct> GetOrderProductDataLoader(
        this IDataLoaderContextAccessor dataLoader,
        IResolveFieldContext context,
        IMediator mediator,
        string loaderKey)
    {
        var loader = dataLoader.Context.GetOrAddBatchLoader<string, ExpProduct>(loaderKey, async ids =>
        {
            // try load products from snapshots then load the missing products by mediator GetProducts query
            var orderAggregate = context.GetOrder();
            var order = orderAggregate.Order;

            var result = await LoadSnapshotProductsAsync(context, ids, order.Id);

            var productIds = ids.Except(result.Keys).ToArray();
            if (productIds.Length > 0)
            {
                // Get currencies and store only from one order.
                // We intentionally ignore the case when there may be orders with different currencies and stores in the resulting set
                var userId = context.GetArgumentOrValue<string>("userId") ?? context.GetCurrentUserId();
                var cultureName = context.GetArgumentOrValue<string>("cultureName") ?? order.LanguageCode;

                var request = new LoadProductsQuery
                {
                    UserId = userId,
                    StoreId = order.StoreId,
                    CurrencyCode = order.Currency,
                    CultureName = cultureName,
                    ObjectIds = productIds,
                    IncludeFields = context.SubFields.Values.GetAllNodesPaths(context).ToArray(),
                };
                context.UserContext.TryAdd("currencyCode", order.Currency);
                context.UserContext.TryAdd("storeId", order.StoreId);
                context.UserContext.TryAdd("store", orderAggregate.Store);
                context.UserContext.TryAdd("cultureName", cultureName);

                var loadProductsResponse = await mediator.Send(request);

                foreach (var product in loadProductsResponse.Products)
                {
                    result.TryAdd(product.Id, product);
                }
            }

            return result;
        });

        return loader;
    }

    private static async Task<Dictionary<string, ExpProduct>> LoadSnapshotProductsAsync(IResolveFieldContext context, IEnumerable<string> productIds, string orderId)
    {
        var products = new List<ExpProduct>();

        var moduleCatalog = context.RequestServices.GetService<IModuleCatalog>();
        if (moduleCatalog == null || !moduleCatalog.IsModuleInstalled("VirtoCommerce.ProductSnapshot"))
        {
            return products.ToDictionary(x => x.Id);
        }

        var snapshotProvider = context.RequestServices.GetService<ICatalogProductSnapshotProvider>();
        if (snapshotProvider == null)
        {
            return products.ToDictionary(x => x.Id);
        }

        var orderProductSnapshots = await snapshotProvider.GetOrderProductSnapshotsAsync(orderId);

        foreach (var productId in productIds)
        {
            var catalogProductSnapshot = orderProductSnapshots.FirstOrDefault(x => x.Id == productId);
            if (catalogProductSnapshot != null)
            {
                var expProduct = GetExpProduct(catalogProductSnapshot);
                products.Add(expProduct);
            }
        }

        return products.ToDictionary(x => x.Id);
    }

    private static ExpProduct GetExpProduct(CatalogProduct catalogProduct)
    {
        var result = AbstractTypeFactory<ExpProduct>.TryCreateInstance();

        result.IndexedProduct = catalogProduct;

        return result;
    }
}
