using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.DataLoader;
using MediatR;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Serialization;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Queries;

namespace VirtoCommerce.XOrder.Core.Extensions;

public static class DataLoaderContextAccessorExtensions
{
    private const string SnapshotCacheKey = "productSnapshotCache";

    private static readonly DataLoaderResult<ExpProduct> _defaultProductResult = new((ExpProduct)null);

    public static IDataLoader<string, ExpProduct> GetOrderProductDataLoader(
        this IDataLoaderContextAccessor dataLoader,
        IResolveFieldContext context,
        IMediator mediator,
        string loaderKey)
    {
        var loader = dataLoader.Context.GetOrAddBatchLoader<string, ExpProduct>(loaderKey, async ids =>
        {
            // Get currencies and store only from one order.
            // We intentionally ignore the case when there may be orders with different currencies and stores in the resulting set
            var orderAggregate = context.GetOrder();
            var order = orderAggregate.Order;
            var userId = context.GetArgumentOrValue<string>("userId") ?? context.GetCurrentUserId();

            var cultureName = context.GetArgumentOrValue<string>("cultureName") ?? order.LanguageCode;

            var request = new LoadProductsQuery
            {
                UserId = userId,
                StoreId = order.StoreId,
                CurrencyCode = order.Currency,
                CultureName = cultureName,
                ObjectIds = ids.ToArray(),
                IncludeFields = context.SubFields.Values.GetAllNodesPaths(context).ToArray(),
            };
            context.UserContext.TryAdd("currencyCode", order.Currency);
            context.UserContext.TryAdd("storeId", order.StoreId);
            context.UserContext.TryAdd("store", orderAggregate.Store);
            context.UserContext.TryAdd("cultureName", cultureName);

            var response = await mediator.Send(request);

            return response.Products.ToDictionary(x => x.Id);
        });

        return loader;
    }

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

    public static IDataLoaderResult<ExpProduct> LoadOrderProductWithSnapshot(
        this IDataLoaderContextAccessor dataLoader,
        IResolveFieldContext context,
        IMediator mediator,
        string loaderKey,
        string productId,
        string productSnapshot)
    {
        if (string.IsNullOrEmpty(productId))
        {
            return _defaultProductResult;
        }

        if (!string.IsNullOrEmpty(productSnapshot))
        {
            var cache = GetOrAddSnapshotCache(context);
            // Cache by ProductId — if multiple items share the same product, first snapshot wins.
            // Within a single order this is safe (all snapshots captured at the same time).
            var expProduct = cache.GetOrAdd(productId, _ => DeserializeSnapshot(productSnapshot));
            return new DataLoaderResult<ExpProduct>(expProduct);
        }

        return LoadOrderProduct(dataLoader, context, mediator, loaderKey, productId);
    }

    private static ConcurrentDictionary<string, ExpProduct> GetOrAddSnapshotCache(IResolveFieldContext context)
    {
        if (context.UserContext.TryGetValue(SnapshotCacheKey, out var cacheObj)
            && cacheObj is ConcurrentDictionary<string, ExpProduct> existing)
        {
            return existing;
        }

        var cache = new ConcurrentDictionary<string, ExpProduct>();
        context.UserContext.TryAdd(SnapshotCacheKey, cache);

        // Another thread may have won the race — always return what's in UserContext
        return (ConcurrentDictionary<string, ExpProduct>)context.UserContext[SnapshotCacheKey];
    }

    private static ExpProduct DeserializeSnapshot(string snapshotJson)
    {
        var catalogProduct = ProductJsonSerializer.DeserializePolymorphic<CatalogProduct>(snapshotJson);

        return new ExpProduct
        {
            IndexedProduct = catalogProduct,
        };
    }
}
