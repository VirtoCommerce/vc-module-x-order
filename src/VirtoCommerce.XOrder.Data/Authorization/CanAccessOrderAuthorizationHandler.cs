using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Extensions;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Extensions;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Common;
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
        private readonly IOrganizationMembershipSearchService _organizationMembershipSearchService;
        private readonly ICustomerOrderService _customerOrderService;

        public CanAccessOrderAuthorizationHandler(
            IMemberService memberService,
            IOrganizationMembershipSearchService organizationMembershipSearchService,
            ICustomerOrderService customerOrderService)
        {
            _memberService = memberService;
            _organizationMembershipSearchService = organizationMembershipSearchService;
            _customerOrderService = customerOrderService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanAccessOrderAuthorizationRequirement requirement)
        {
            var result = context.User.IsInRole(PlatformConstants.Security.SystemRoles.Administrator);

            if (!result)
            {
                var resource = context.Resource;

                if (resource is File file)
                {
                    result = file.OwnerIsEmpty();

                    if (!result && file.OwnerTypeIs<CustomerOrder>())
                    {
                        resource = await _customerOrderService.GetByIdAsync(file.OwnerEntityId);
                    }
                }

                if (resource is CustomerOrder order)
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
            if (!MemberAssignedToOrganization(member, organizationId))
            {
                return false;
            }

            var userId = GetCurrentUserId(context);
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            var membership = await _organizationMembershipSearchService.GetMembershipAsync(userId, organizationId);
            return membership?.IsCurrentlyLocked != true;
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
