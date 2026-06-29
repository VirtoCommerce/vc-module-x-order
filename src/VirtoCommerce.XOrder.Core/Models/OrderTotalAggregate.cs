using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.OrdersModule.Core.Model;

namespace VirtoCommerce.XOrder.Core.Models
{
    public class OrderTotalAggregate
    {
        public bool IsDefaultTotalCurrency { get; set; }

        public Currency Currency { get; set; }

        public OrderTotal OrderTotal { get; set; }
    }
}
