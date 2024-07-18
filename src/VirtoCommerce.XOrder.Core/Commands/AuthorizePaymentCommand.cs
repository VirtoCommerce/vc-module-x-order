using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XOrder.Core.Models;

namespace VirtoCommerce.XOrder.Core.Commands
{
    public class AuthorizePaymentCommand : PaymentCommandBase, ICommand<AuthorizePaymentResult>
    {
        public KeyValue[] Parameters { get; set; }
    }
}
