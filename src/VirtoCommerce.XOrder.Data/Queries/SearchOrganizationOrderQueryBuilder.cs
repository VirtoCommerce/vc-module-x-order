using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XOrder.Core.Queries;
using VirtoCommerce.XOrder.Data.Queries.BaseQueries;

namespace VirtoCommerce.XOrder.Data.Queries
{
    public class SearchOrganizationOrderQueryBuilder : BaseSearchOrderQueryBuilder<SearchOrganizationOrderQuery>
    {
        protected override string Name => "organizationOrders";

        public SearchOrganizationOrderQueryBuilder(IMediator mediator, IAuthorizationService authorizationService, ICurrencyService currencyService, IUserManagerCore userManagerCore)
            : base(mediator, authorizationService, currencyService, userManagerCore)
        {
        }
    }
}
