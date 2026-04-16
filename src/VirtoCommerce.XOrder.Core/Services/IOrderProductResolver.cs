using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XOrder.Core.Models;

namespace VirtoCommerce.XOrder.Core.Services;

public interface IOrderProductResolver
{
    Task<IDictionary<string, ExpProduct>> ResolveOrderProductsAsync(string orderId, IList<string> productIds, OrderProductResolveContext context);
}
