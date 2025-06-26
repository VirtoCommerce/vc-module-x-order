using System.Collections.Generic;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Models;

namespace VirtoCommerce.XOrder.Core.Models
{
    public class ExpOrderPayment
    {
        public Optional<string> Id { get; set; }
        public Optional<string> OuterId { get; set; }
        public Optional<string> PaymentGatewayCode { get; set; }
        public Optional<string> Currency { get; set; }
        public Optional<decimal> Price { get; set; }
        public Optional<decimal> Amount { get; set; }
        public Optional<string> Comment { get; set; }
        public Optional<string> VendorId { get; set; }

        public Optional<ExpOrderAddress> BillingAddress { get; set; }

        public IList<DynamicPropertyValue> DynamicProperties { get; set; }

        public virtual PaymentIn MapTo(PaymentIn payment)
        {
            payment ??= AbstractTypeFactory<PaymentIn>.TryCreateInstance();

            Optional.SetValue(Id, x => payment.Id = x);
            Optional.SetValue(OuterId, x => payment.OuterId = x);
            Optional.SetValue(PaymentGatewayCode, x => payment.GatewayCode = x);
            Optional.SetValue(Currency, x => payment.Currency = x);
            Optional.SetValue(Price, x => payment.Price = x);
            Optional.SetValue(Amount, x => payment.Sum = x);
            Optional.SetValue(VendorId, x => payment.VendorId = x);
            Optional.SetValue(Comment, x => payment.Comment = x);
            Optional.SetValue(BillingAddress, x => payment.BillingAddress = x?.MapTo(payment.BillingAddress));

            return payment;
        }
    }
}
