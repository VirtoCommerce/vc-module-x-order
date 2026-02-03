using VirtoCommerce.PaymentModule.Core.Model;

namespace VirtoCommerce.XOrder.Tests.Helpers.Stubs
{
    public class StubPaymentMethod(string code) : PaymentMethod(code)
    {
        public override PaymentMethodType PaymentMethodType => throw new System.NotImplementedException();

        public override PaymentMethodGroupType PaymentMethodGroupType => throw new System.NotImplementedException();
    }
}
