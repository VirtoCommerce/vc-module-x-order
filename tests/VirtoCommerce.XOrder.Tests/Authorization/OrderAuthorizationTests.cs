using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.XOrder.Core.Queries;
using VirtoCommerce.XOrder.Data.Authorization;
using Xunit;

namespace VirtoCommerce.XOrder.Tests.Authorization
{
    public class OrderAuthorizationTests
    {
        private readonly Mock<IMemberService> _memberServiceMock;
        private readonly Mock<IOrganizationMembershipSearchService> _organizationMembershipSearchServiceMock;

        static OrderAuthorizationTests()
        {
            ClaimsPrincipalExtensions.UserIdClaimTypes = ["name"];
        }

        public OrderAuthorizationTests()
        {
            ClaimsPrincipalExtensions.UserIdClaimTypes = ["name"];

            _memberServiceMock = new Mock<IMemberService>();

            _memberServiceMock
                .Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string memberId, string responseGroup, string memberType) => new Contact() { Organizations = ["organization1", "organization2"] });

            _organizationMembershipSearchServiceMock = new Mock<IOrganizationMembershipSearchService>();

            _organizationMembershipSearchServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<OrganizationMembershipSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new OrganizationMembershipSearchResult { Results = [] });
        }

        [Fact]
        public async Task CanAccessOrderAuthorizationHandler_OrderBelongUser_ShouldSucceed()
        {
            //Arrange
            var requirements = new[] { new CanAccessOrderAuthorizationRequirement() };
            var userId = "userId";
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", userId)]));
            //var mockService =

            var resource = new CustomerOrder { CustomerId = "userId" };

            var context = new AuthorizationHandlerContext(requirements, user, resource);
            var subject = CreateHandler();

            //Act
            await subject.HandleAsync(context);

            //Assert
            context.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task CanAccessOrderAuthorizationHandler_OrderBelongUserOrganization_ShouldSucceed()
        {
            //Arrange    
            var requirements = new[] { new CanAccessOrderAuthorizationRequirement() };
            var userId = "userId";
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", userId), new Claim("memberId", "memberId")]));

            var resource = new CustomerOrder { CustomerId = "AnotherUserId", OrganizationId = "organization1" };

            var context = new AuthorizationHandlerContext(requirements, user, resource);
            var subject = CreateHandler();

            //Act
            await subject.HandleAsync(context);

            //Assert
            context.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task CanAccessOrderAuthorizationHandler_OrderBelongAnotherUser_ShouldFail()
        {
            //Arrange    
            var requirements = new[] { new CanAccessOrderAuthorizationRequirement() };
            var userId = "userId";
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", userId)]));

            var resource = new CustomerOrder { CustomerId = "AnotherUserId" };

            var context = new AuthorizationHandlerContext(requirements, user, resource);
            var subject = CreateHandler();

            //Act
            await subject.HandleAsync(context);

            //Assert
            context.HasFailed.Should().BeTrue();
        }

        public static IEnumerable<object[]> GetSearchOrderQuery()
        {
            yield return new object[] { new SearchCustomerOrderQuery { CustomerId = "userId" } };
            yield return new object[] { new SearchCustomerOrderQuery { CustomerId = null } };
        }

        [Theory]
        [MemberData(nameof(GetSearchOrderQuery))]
        public async Task CanAccessOrderAuthorizationHandler_SearchOrdersBelongToUser_ShouldSucceed(SearchCustomerOrderQuery query)
        {
            //Arrange
            var requirements = new[] { new CanAccessOrderAuthorizationRequirement() };
            var userId = "userId";
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", userId)]));

            var context = new AuthorizationHandlerContext(requirements, user, query);
            var subject = CreateHandler();

            //Act
            await subject.HandleAsync(context);

            //Assert
            context.HasSucceeded.Should().BeTrue();
            query.CustomerId.Should().Be("userId");
        }

        [Theory]
        [MemberData(nameof(GetSearchOrderQuery))]
        public async Task CanAccessOrderAuthorizationHandler_SearchOrdersWithoutAuth_ShouldFail(SearchCustomerOrderQuery query)
        {
            //Arrange    
            var requirements = new[] { new CanAccessOrderAuthorizationRequirement() };

            var user = new ClaimsPrincipal(new ClaimsIdentity());

            var context = new AuthorizationHandlerContext(requirements, user, query);
            var subject = CreateHandler();

            //Act
            await subject.HandleAsync(context);

            //Assert
            context.HasFailed.Should().BeTrue();
        }


        public static IEnumerable<object[]> GetSearchPaymentsQuery()
        {
            yield return new object[] { new SearchPaymentsQuery { CustomerId = "userId" } };
            yield return new object[] { new SearchPaymentsQuery { CustomerId = null } };
        }

        [Theory]
        [MemberData(nameof(GetSearchPaymentsQuery))]
        public async Task CanAccessOrderAuthorizationHandler_SearchPaymentsBelongToUser_ShouldSucceed(SearchPaymentsQuery query)
        {
            //Arrange    
            var requirements = new[] { new CanAccessOrderAuthorizationRequirement() };
            var userId = "userId";
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", userId)]));

            var context = new AuthorizationHandlerContext(requirements, user, query);
            var subject = CreateHandler();

            //Act
            await subject.HandleAsync(context);

            //Assert
            context.HasSucceeded.Should().BeTrue();
            query.CustomerId.Should().Be("userId");
        }

        [Theory]
        [MemberData(nameof(GetSearchPaymentsQuery))]
        public async Task CanAccessOrderAuthorizationHandler_SearchPaymentsWithoutAuth_ShouldFail(SearchPaymentsQuery query)
        {
            //Arrange    
            var requirements = new[] { new CanAccessOrderAuthorizationRequirement() };

            var user = new ClaimsPrincipal(new ClaimsIdentity());

            var context = new AuthorizationHandlerContext(requirements, user, query);
            var subject = CreateHandler();

            //Act
            await subject.HandleAsync(context);

            //Assert
            context.HasFailed.Should().BeTrue();
        }

        public static readonly IList<object[]> SearchOrganizationOrderQueryTestData =
        [
            [new SearchOrganizationOrderQuery { OrganizationId = "organization1" }, true],
            [new SearchOrganizationOrderQuery { OrganizationId = "organization3" }, false]
        ];

        [Theory]
        [MemberData(nameof(SearchOrganizationOrderQueryTestData))]
        public async Task CanAccessOrderAuthorizationHandler_SearchOrganizationOrderQuery(SearchOrganizationOrderQuery query, bool succeeded)
        {
            //Arrange
            var requirements = new[] { new CanAccessOrderAuthorizationRequirement() };

            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", "userId"), new Claim("memberId", "memberId")]));

            var context = new AuthorizationHandlerContext(requirements, user, query);
            var subject = CreateHandler();

            //Act
            await subject.HandleAsync(context);

            //Assert
            Assert.Equal(succeeded, context.HasSucceeded);
        }

        [Fact]
        public async Task CanAccessOrderAuthorizationHandler_SearchOrganizationOrderQuery_LockedMembership_ShouldFail()
        {
            //Arrange
            var requirements = new[] { new CanAccessOrderAuthorizationRequirement() };
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", "userId"), new Claim("memberId", "memberId")]));
            var query = new SearchOrganizationOrderQuery { OrganizationId = "organization1" };

            // The member is still assigned to the organization (Contact.Organizations still lists it),
            // but their membership has since been locked — this is exactly the VCST-5933 scenario.
            _organizationMembershipSearchServiceMock
                .Setup(x => x.SearchAsync(
                    It.Is<OrganizationMembershipSearchCriteria>(c => c.UserId == "userId" && c.OrganizationId == "organization1"),
                    It.IsAny<bool>()))
                .ReturnsAsync(new OrganizationMembershipSearchResult
                {
                    Results =
                    [
                        new() { UserId = "userId", OrganizationId = "organization1", IsLocked = true }
                    ]
                });

            var context = new AuthorizationHandlerContext(requirements, user, query);
            var subject = CreateHandler();

            //Act
            await subject.HandleAsync(context);

            //Assert
            context.HasFailed.Should().BeTrue();
        }

        [Fact]
        public async Task CanAccessOrderAuthorizationHandler_SearchOrganizationOrderQuery_UnlockedMembership_ShouldSucceed()
        {
            //Arrange
            var requirements = new[] { new CanAccessOrderAuthorizationRequirement() };
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", "userId"), new Claim("memberId", "memberId")]));
            var query = new SearchOrganizationOrderQuery { OrganizationId = "organization1" };

            _organizationMembershipSearchServiceMock
                .Setup(x => x.SearchAsync(
                    It.Is<OrganizationMembershipSearchCriteria>(c => c.UserId == "userId" && c.OrganizationId == "organization1"),
                    It.IsAny<bool>()))
                .ReturnsAsync(new OrganizationMembershipSearchResult
                {
                    Results =
                    [
                        new() { UserId = "userId", OrganizationId = "organization1", IsLocked = false }
                    ]
                });

            var context = new AuthorizationHandlerContext(requirements, user, query);
            var subject = CreateHandler();

            //Act
            await subject.HandleAsync(context);

            //Assert
            context.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task CanAccessOrderAuthorizationHandler_SearchOrganizationOrderQuery_QueriesMembershipByUserIdNotMemberId()
        {
            //Arrange — "name"/userId and "memberId" claims are deliberately different values, so a handler
            // that mistakenly queries membership by memberId would never match this setup and would fail closed.
            var requirements = new[] { new CanAccessOrderAuthorizationRequirement() };
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", "userId"), new Claim("memberId", "memberId")]));
            var query = new SearchOrganizationOrderQuery { OrganizationId = "organization1" };

            _organizationMembershipSearchServiceMock
                .Setup(x => x.SearchAsync(
                    It.Is<OrganizationMembershipSearchCriteria>(c => c.UserId == "userId"),
                    It.IsAny<bool>()))
                .ReturnsAsync(new OrganizationMembershipSearchResult { Results = [] });

            var context = new AuthorizationHandlerContext(requirements, user, query);
            var subject = CreateHandler();

            //Act
            await subject.HandleAsync(context);

            //Assert
            context.HasSucceeded.Should().BeTrue();
            _organizationMembershipSearchServiceMock.Verify(
                x => x.SearchAsync(It.Is<OrganizationMembershipSearchCriteria>(c => c.UserId == "userId"), It.IsAny<bool>()),
                Times.Once);
        }

        private CanAccessOrderAuthorizationHandler CreateHandler()
        {
            return new CanAccessOrderAuthorizationHandler(_memberServiceMock.Object, _organizationMembershipSearchServiceMock.Object);
        }
    }
}
