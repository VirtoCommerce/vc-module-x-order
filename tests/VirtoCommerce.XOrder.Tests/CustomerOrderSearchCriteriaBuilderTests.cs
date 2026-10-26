using FluentAssertions;
using Moq;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.XOrder.Data.Services;
using Xunit;

namespace VirtoCommerce.XOrder.Tests;

public class CustomerOrderSearchCriteriaBuilderTests
{
    // The caller's scope (customer/organization) must land in criteria fields that are AND-ed as top-level filters,
    // never concatenated into the parsed Keyword. Otherwise a crafted user filter with an OR operator
    // (e.g. 'OR status:"Processing"') re-parses the scope terms as OR alternatives and returns other customers' orders.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("status:\"Processing\"")]
    [InlineData("OR status:\"Processing\"")]
    [InlineData("OR")]
    public void Scope_IsHeldInCriteriaFields_NotInTheParsedKeyword(string userFilter)
    {
        var builder = new CustomerOrderSearchCriteriaBuilder(Mock.Of<ISearchPhraseParser>())
            .ParseFilters(userFilter)
            .WithCustomerId("the-buyer")
            .WithOrganizationId("the-org");

        var criteria = builder.Build();

        // Scope is carried as fields (CustomerOrderSearchRequestBuilder turns these into AND-ed term filters).
        criteria.CustomerId.Should().Be("the-buyer");
        criteria.OrganizationId.Should().Be("the-org");
        criteria.CustomerIds.Should().Contain("the-buyer");
        criteria.OrganizationIds.Should().Contain("the-org");

        // The user's text is the only thing in the parsed keyword, so no keyword can restructure the scope.
        criteria.Keyword.Should().Be(userFilter);
        if (!string.IsNullOrEmpty(userFilter))
        {
            criteria.Keyword.Should().NotContain("the-buyer").And.NotContain("the-org");
        }
    }
}
