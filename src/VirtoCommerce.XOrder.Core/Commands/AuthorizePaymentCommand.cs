using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XOrder.Core.Models;

namespace VirtoCommerce.XOrder.Core.Commands
{
    public class AuthorizePaymentCommand : PaymentCommandBase, ICommand<AuthorizePaymentResult>
    {
    }
}
