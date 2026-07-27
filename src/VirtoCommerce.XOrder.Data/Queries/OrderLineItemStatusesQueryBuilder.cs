using System;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.Queries;
using VirtoCommerce.XOrder.Core.Queries;

namespace VirtoCommerce.XOrder.Data.Queries;

public class OrderLineItemStatusesQueryBuilder : LocalizedSettingQueryBuilder<OrderLineItemStatusesQuery>
{
    public OrderLineItemStatusesQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    [Obsolete("Use the constructor without IMediator. The mediator is resolved from context.RequestServices per request.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public OrderLineItemStatusesQueryBuilder(IMediator mediator, IAuthorizationService authorizationService)
        : this(authorizationService)
    {
    }

    protected override string Name => "OrderLineItemStatuses";
}
