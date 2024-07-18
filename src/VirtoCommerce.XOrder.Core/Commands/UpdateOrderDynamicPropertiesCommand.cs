using System.Collections.Generic;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Models;

namespace VirtoCommerce.XOrder.Core.Commands
{
    public class UpdateOrderDynamicPropertiesCommand : ICommand<CustomerOrderAggregate>
    {
        public string OrderId { get; set; }

        public IList<DynamicPropertyValue> DynamicProperties { get; set; }
    }
}
