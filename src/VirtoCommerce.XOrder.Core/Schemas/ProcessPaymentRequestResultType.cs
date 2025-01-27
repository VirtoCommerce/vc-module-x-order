using GraphQL.Types;
using VirtoCommerce.PaymentModule.Model.Requests;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XOrder.Core.Schemas
{
    public class ProcessPaymentRequestResultType : ExtendableGraphType<ProcessPaymentRequestResult>
    {
        public ProcessPaymentRequestResultType()
        {
            Field(x => x.IsSuccess);
            Field(x => x.HtmlForm, nullable: true);
            Field<StringGraphType>("newPaymentStatus")
                .Description("New payment status")
                .Resolve(context => context.Source.NewPaymentStatus.ToString());
            Field(x => x.OuterId, nullable: true);
            Field(x => x.RedirectUrl, nullable: true);
            Field(x => x.ErrorMessage, nullable: true);
        }
    }
}
