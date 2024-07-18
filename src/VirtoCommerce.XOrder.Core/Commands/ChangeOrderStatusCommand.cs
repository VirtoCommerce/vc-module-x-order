using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.XOrder.Core.Commands
{
    public class ChangeOrderStatusCommand : ICommand<bool>
    {
        public string OrderId { get; set; }
        public string Status { get; set; }
    }
}
