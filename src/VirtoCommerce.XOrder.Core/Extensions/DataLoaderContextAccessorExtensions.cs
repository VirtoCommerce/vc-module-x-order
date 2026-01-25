using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.DataLoader;
using MediatR;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Queries;

namespace VirtoCommerce.XOrder.Core.Extensions;

public static class DataLoaderContextAccessorExtensions
{
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
            var orderAggregate = context.GetValueForSource<CustomerOrderAggregate>();
            var order = orderAggregate.Order;
            var userId = context.GetArgumentOrValue<string>("userId") ?? context.GetCurrentUserId();

            var request = new LoadProductsQuery
            {
                UserId = userId,
                StoreId = order.StoreId,
                CurrencyCode = order.Currency,
                ObjectIds = ids.ToArray(),
                IncludeFields = context.SubFields.Values.GetAllNodesPaths(context).ToArray(),
            };

            var cultureName = context.GetArgumentOrValue<string>("cultureName") ?? order.LanguageCode;
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
}
