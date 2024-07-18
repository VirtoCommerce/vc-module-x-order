using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Model.Requests;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.XOrder.Core.Commands
{
    public class ProcessOrderPaymentCommand : ICommand<ProcessPaymentRequestResult>
    {
        public string OrderId { get; set; }
        public string PaymentId { get; set; }
        public BankCardInfo BankCardInfo { get; set; }
    }
}
