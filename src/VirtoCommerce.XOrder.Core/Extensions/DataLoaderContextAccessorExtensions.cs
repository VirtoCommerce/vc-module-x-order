using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.DataLoader;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XOrder.Core.Services;

namespace VirtoCommerce.XOrder.Core.Extensions;

public static class DataLoaderContextAccessorExtensions
{
    private static readonly DataLoaderResult<ExpProduct> _defaultProductResult = new((ExpProduct)null);

    extension(IDataLoaderContextAccessor dataLoader)
    {
        public IDataLoaderResult<ExpProduct> LoadOrderProductWithSnapshot(IResolveFieldContext context, string loaderKey, string productId)
        {
            if (string.IsNullOrEmpty(productId))
            {
                return _defaultProductResult;
            }

            var loader = dataLoader.GetOrderProductDataLoader(context, loaderKey);

            return loader.LoadAsync(productId);
        }

        public IDataLoader<string, ExpProduct> GetOrderProductDataLoader(IResolveFieldContext context, string loaderKey)
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

                var resolveContext = context.CreateOrderProductResolveContext(orderAggregate);
                resolveContext.IncludeFields = context.SubFields.Values.GetAllNodesPaths(context).ToArray();

                var productResolver = context.RequestServices.GetRequiredService<IOrderProductResolver>();
                var result = await productResolver.ResolveOrderProductsAsync(order.Id, ids.ToList(), resolveContext);

                return result;
            });

            return loader;
        }
    }
}
