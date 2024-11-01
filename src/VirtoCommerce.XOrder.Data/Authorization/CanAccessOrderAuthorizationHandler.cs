using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Security.Authorization;
using VirtoCommerce.XOrder.Core.Queries;

namespace VirtoCommerce.XOrder.Data.Authorization
{
    public sealed class CanAccessOrderAuthorizationRequirement : PermissionAuthorizationRequirement
    {
        public CanAccessOrderAuthorizationRequirement() : base("CanAccessOrder")
        {
        }
    }

    public class CanAccessOrderAuthorizationHandler : PermissionAuthorizationHandlerBase<CanAccessOrderAuthorizationRequirement>
    {
        private readonly IMemberService _memberService;

        public CanAccessOrderAuthorizationHandler(IMemberService memberService)
        {
            _memberService = memberService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanAccessOrderAuthorizationRequirement requirement)
        {
            var result = context.User.IsInRole(PlatformConstants.Security.SystemRoles.Administrator);

            if (!result)
            {
                if (context.Resource is CustomerOrder order)
                {
                    var currentUserId = GetCurrentUserId(context);
                    result = currentUserId == null && order.IsAnonymous ||
                        order.CustomerId == currentUserId ||
                        await IsCustomerOrganization(context, order.OrganizationId);
                }
                else if (context.Resource is SearchCustomerOrderQuery query)
                {
                    query.CustomerId = GetCurrentUserId(context);
                    result = query.CustomerId != null;
                }
                else if (context.Resource is SearchOrganizationOrderQuery organizationOrderQuery)
                {
                    result = await IsCustomerOrganization(context, organizationOrderQuery.OrganizationId);
                }
                else if (context.Resource is SearchPaymentsQuery paymentsQuery)
                {
                    paymentsQuery.CustomerId = GetCurrentUserId(context);
                    result = paymentsQuery.CustomerId != null;
                }
                else if (context.Resource is ShoppingCart cart)
                {
                    var currentUserId = GetCurrentUserId(context);
                    result = cart.CustomerId == currentUserId || currentUserId == null && cart.IsAnonymous;
                }
            }

            if (result)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }

        protected virtual async Task<bool> IsCustomerOrganization(AuthorizationHandlerContext context, string organizationId)
        {
            var memberId = GetMemberId(context);
            if (string.IsNullOrEmpty(organizationId) || string.IsNullOrEmpty(memberId))
            {
                return false;
            }

            var member = await _memberService.GetByIdAsync(memberId);
            return MemberAssignedToOrganization(member, organizationId);
        }

        private static string GetCurrentUserId(AuthorizationHandlerContext context)
        {
            return context.User.GetUserId();
        }

        private static string GetMemberId(AuthorizationHandlerContext context)
        {
            return context.User.FindFirstValue("memberId");
        }

        private static bool MemberAssignedToOrganization(Member member, string organizationId)
        {
            return member?.MemberType switch
            {
                nameof(Contact) => (member as Contact)?.Organizations?.Contains(organizationId) ?? false,
                nameof(Employee) => (member as Employee)?.Organizations?.Contains(organizationId) ?? false,
                _ => false
            };
        }
    }
}
