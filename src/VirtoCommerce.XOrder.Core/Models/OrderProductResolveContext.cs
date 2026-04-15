using System.Collections.Generic;

namespace VirtoCommerce.XOrder.Core.Models;

public class OrderProductResolveContext
{
    public string UserId { get; set; }
    public string OrganizationId { get; set; }
    public string StoreId { get; set; }
    public string CurrencyCode { get; set; }
    public string CultureName { get; set; }
    public IList<string> IncludeFields { get; set; } = [];
}
