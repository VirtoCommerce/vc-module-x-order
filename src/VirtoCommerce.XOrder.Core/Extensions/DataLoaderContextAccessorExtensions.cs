using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.DataLoader;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Queries;
using VirtoCommerce.XOrder.Core.Models;

namespace VirtoCommerce.XOrder.Core.Extensions;

public static class DataLoaderContextAccessorExtensions
{
    private static readonly DataLoaderResult<ExpProduct> _defaultProductResult = new((ExpProduct)null);

    public static IDataLoaderResult<ExpProduct> LoadOrderProductWithSnapshot(
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
            var orderAggregate = context.GetOrder();
            var order = orderAggregate.Order;

            // Get currencies and store only from one order.
            // We intentionally ignore the case when there may be orders with different currencies and stores in the resulting set
            var cultureName = context.GetArgumentOrValue<string>("cultureName") ?? order.LanguageCode;
            context.UserContext.TryAdd("currencyCode", order.Currency);
            context.UserContext.TryAdd("storeId", order.StoreId);
            context.UserContext.TryAdd("store", orderAggregate.Store);
            context.UserContext.TryAdd("cultureName", cultureName);

            // try load products from snapshots then load the missing products by mediator GetProducts query
            var result = await LoadSnapshotProductsAsync(context, ids, order.Id);

            var productIds = result.Where(x => x.Value == null).Select(x => x.Key).ToArray();
            if (productIds.Length > 0)
            {
                var userId = context.GetArgumentOrValue<string>("userId") ?? context.GetCurrentUserId();
                var request = new LoadProductsQuery
                {
                    UserId = userId,
                    StoreId = order.StoreId,
                    CurrencyCode = order.Currency,
                    CultureName = cultureName,
                    ObjectIds = productIds,
                    IncludeFields = context.SubFields.Values.GetAllNodesPaths(context).ToArray(),
                };

                var loadProductsResponse = await mediator.Send(request);

                foreach (var product in loadProductsResponse.Products)
                {
                    if (result.TryGetValue(product.Id, out var value) && value == null)
                    {
                        result[product.Id] = product;
                    }
                }
            }

            return result;
        });

        return loader;
    }

    private static async Task<IDictionary<string, ExpProduct>> LoadSnapshotProductsAsync(IResolveFieldContext context, IEnumerable<string> productIds, string orderId)
    {
        var products = productIds.Distinct().ToDictionary(x => x, x => default(ExpProduct));

        var pipeline = context.RequestServices.GetService<IGenericPipelineLauncher>();
        if (pipeline == null)
        {
            return products;
        }

        var externalOrdeProducts = new ExternalOrderProducts
        {
            OrderId = orderId,
            Products = products,
        };

        await pipeline.Execute(externalOrdeProducts);

        return externalOrdeProducts.Products;
    }
}
