using System.Linq;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XOrder.Core.Models;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class InitializePaymentResultType : ExtendableGraphType<InitializePaymentResult>
    {
        public InitializePaymentResultType()
        {
            Field(x => x.IsSuccess);
            Field(x => x.ErrorMessage, nullable: true);
            Field(x => x.StoreId, nullable: true);
            Field(x => x.PaymentId, nullable: true);
            Field(x => x.OrderId, nullable: true);
            Field(x => x.OrderNumber, nullable: true);
            Field(x => x.PaymentMethodCode, nullable: true);
            Field(x => x.PaymentActionType, nullable: true);
            Field(x => x.ActionRedirectUrl, nullable: true);
            Field(x => x.ActionHtmlForm, nullable: true);
            Field<ListGraphType<KeyValueType>>(nameof(InitializePaymentResult.PublicParameters).ToCamelCase()).Resolve(context =>
                context.Source.PublicParameters?.Select(x => new KeyValue { Key = x.Key, Value = x.Value }));
        }
    }
}
