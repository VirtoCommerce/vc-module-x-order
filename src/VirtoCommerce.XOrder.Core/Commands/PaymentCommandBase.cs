using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.XOrder.Core.Commands
{
    public class PaymentCommandBase
    {
        public string OrderId { get; set; }

        public string PaymentId { get; set; }

        public string StoreId { get; set; }

        public string CultureName { get; set; }

        public KeyValue[] Parameters { get; set; }
    }
}
