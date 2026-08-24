using System;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XOrder.Core.Queries;
using VirtoCommerce.XOrder.Data.Queries.BaseQueries;

namespace VirtoCommerce.XOrder.Data.Queries
{
    public class SearchOrderQueryBuilder : BaseSearchOrderQueryBuilder<SearchCustomerOrderQuery>
    {
        protected override string Name => "orders";

        public SearchOrderQueryBuilder(IAuthorizationService authorizationService, ICurrencyService currencyService, IUserManagerCore userManagerCore)
            : base(authorizationService, currencyService, userManagerCore)
        {
        }

        [Obsolete("Use the constructor without IMediator. The mediator is resolved from context.RequestServices per request.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
        public SearchOrderQueryBuilder(IMediator mediator, IAuthorizationService authorizationService, ICurrencyService currencyService, IUserManagerCore userManagerCore)
            : this(authorizationService, currencyService, userManagerCore)
        {
        }
    }
}
