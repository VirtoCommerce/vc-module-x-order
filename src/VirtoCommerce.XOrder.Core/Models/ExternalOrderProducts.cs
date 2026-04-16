using System.Collections.Generic;
using VirtoCommerce.XCatalog.Core.Models;

namespace VirtoCommerce.XOrder.Core.Models;

public class ExternalOrderProducts
{
    public string OrderId { get; set; }

    public IList<string> ProductIds { get; set; } = [];

    public IList<ExpProduct> Products { get; set; } = [];
}
