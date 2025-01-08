using GraphQL;
using GraphQL.Types;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XOrder.Core.Extensions;
using Money = VirtoCommerce.CoreModule.Core.Currency.Money;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class PaymentTransactionType : ExtendableGraphType<PaymentGatewayTransaction>
    {
        public PaymentTransactionType()
        {
            Field(x => x.Id, nullable: false);
            Field(x => x.IsProcessed, nullable: false);
            Field(x => x.ProcessedDate, nullable: true);
            Field(x => x.ProcessError, nullable: true);
            Field(x => x.ProcessAttemptCount, nullable: false);
            Field(x => x.RequestData, nullable: true);
            Field(x => x.ResponseData, nullable: true);
            Field(x => x.ResponseCode, nullable: true);
            Field(x => x.GatewayIpAddress, nullable: true);
            Field(x => x.Type, nullable: true);
            Field(x => x.Status, nullable: true);
            Field(x => x.Note, nullable: true);

            Field<NonNullGraphType<MoneyType>>(nameof(PaymentGatewayTransaction.Amount).ToCamelCase()).Resolve(context => new Money(context.Source.Amount, context.GetOrderCurrencyByCode(context.Source.CurrencyCode)));
        }
    }
}
