using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.XOrder.Core.Commands
{
    public class TransferOrderCommand : ICommand<CustomerOrderAggregate>
    {
        public string CustomerOrderId { get; set; }
        public string ToUserId { get; set; }
        public string UserName { get; set; }
    }
}
