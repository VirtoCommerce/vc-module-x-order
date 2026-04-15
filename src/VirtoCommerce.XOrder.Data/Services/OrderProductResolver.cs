using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Queries;
using VirtoCommerce.XOrder.Core.Models;
using VirtoCommerce.XOrder.Core.Services;

namespace VirtoCommerce.XOrder.Data.Services;

public class OrderProductResolver(IGenericPipelineLauncher pipeline, IMediator mediator) : IOrderProductResolver
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ExpProduct>> _cache = new();

    private static readonly string[] _defaultIncludeFields = ["properties", "images", "descriptions"];
    protected virtual string[] DefaultIncludeFields => _defaultIncludeFields;

    public virtual async Task<IDictionary<string, ExpProduct>> ResolveOrderProductsAsync(
        string orderId,
        IList<string> productIds,
        OrderProductResolveContext context)
    {
        ArgumentException.ThrowIfNullOrEmpty(orderId);
        ArgumentNullException.ThrowIfNull(productIds);
        ArgumentNullException.ThrowIfNull(context);

        var orderCache = _cache.GetOrAdd(orderId, _ => new ConcurrentDictionary<string, ExpProduct>());

        var uncached = productIds
            .Where(id => !orderCache.ContainsKey(id))
            .ToList();

        if (uncached.Count > 0)
        {
            // ExternalOrderProducts pipeline (snapshots, etc.)
            var external = new ExternalOrderProducts
            {
                OrderId = orderId,
                ProductIds = uncached,
            };
            await pipeline.Execute(external);

            foreach (var product in external.Products ?? [])
            {
                orderCache.TryAdd(product.Id, product);
            }

            var missing = uncached
                .Where(id => !orderCache.ContainsKey(id))
                .ToArray();

            if (missing.Length > 0)
            {
                var products = await LoadProductsAsync(missing, context);

                foreach (var product in products ?? [])
                {
                    orderCache.TryAdd(product.Id, product);
                }
            }
        }

        return productIds
            .Select(id => (id, Product: orderCache.GetValueOrDefault(id)))
            .Where(x => x.Product is not null)
            .ToDictionary(x => x.id, x => x.Product!);
    }

    /// <summary>
    /// Fallback for products not provided by the ExternalOrderProducts pipeline.
    /// Default: LoadProductsQuery (catalog via XCatalog pipeline).
    /// Override to use IItemService or another source.
    /// </summary>
    protected virtual async Task<IList<ExpProduct>> LoadProductsAsync(IList<string> productIds, OrderProductResolveContext context)
    {
        var includeFields = context.IncludeFields is { Count: > 0 }
            ? context.IncludeFields.ToArray()
            : DefaultIncludeFields;

        var response = await mediator.Send(new LoadProductsQuery
        {
            ObjectIds = productIds,
            UserId = context.UserId,
            StoreId = context.StoreId,
            CurrencyCode = context.CurrencyCode,
            CultureName = context.CultureName,
            IncludeFields = includeFields,
        });

        return response.Products?.ToList();
    }
}
