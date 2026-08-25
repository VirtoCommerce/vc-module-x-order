using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Moq;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Model.Search;
using VirtoCommerce.OrdersModule.Core.Search.Indexed;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.Xapi.Core.Models.Facets;
using VirtoCommerce.XOrder.Core.Queries;
using VirtoCommerce.XOrder.Core.Services;
using VirtoCommerce.XOrder.Data.Queries;
using VirtoCommerce.XOrder.Data.Services;
using Xunit;

namespace VirtoCommerce.XOrder.Tests.Queries;

public class SearchOrderQueryHandlerTests
{
    private readonly Mock<ISearchPhraseParser> _searchPhraseParserMock = new();
    private readonly Mock<ICustomerOrderAggregateRepository> _aggregateRepositoryMock = new();
    private readonly Mock<IIndexedCustomerOrderSearchService> _searchServiceMock = new();
    private readonly Mock<IXOrderMapper> _mapperMock = new();

    private SearchOrderQueryHandler CreateHandler()
    {
        return new SearchOrderQueryHandler(
            _searchPhraseParserMock.Object,
            _aggregateRepositoryMock.Object,
            _searchServiceMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_MultipleAggregations_BuildsFacetMappingContextOnce()
    {
        // Every aggregation in one response must share the same context instance.
        var aggregations = new List<OrderAggregation>
        {
            new() { AggregationType = "attr", Field = "status" },
            new() { AggregationType = "attr", Field = "paymentMethod" },
        };

        _searchServiceMock
            .Setup(x => x.SearchCustomerOrdersAsync(It.IsAny<CustomerOrderIndexedSearchCriteria>()))
            .ReturnsAsync(new CustomerOrderIndexedSearchResult
            {
                TotalCount = 0,
                Results = [],
                Aggregations = aggregations,
            });

        _aggregateRepositoryMock
            .Setup(x => x.GetAggregatesFromOrdersAsync(It.IsAny<IList<CustomerOrder>>(), It.IsAny<string>()))
            .ReturnsAsync([]);

        _mapperMock
            .Setup(x => x.CreateFacetMappingContext("en-US"))
            .Returns(new FacetMappingContext { CultureName = "en-US" });

        var capturedContexts = new List<FacetMappingContext>();
        _mapperMock
            .Setup(x => x.ToFacetResult(It.IsAny<OrderAggregation>(), It.IsAny<FacetMappingContext>()))
            .Returns<OrderAggregation, FacetMappingContext>((_, context) =>
            {
                capturedContexts.Add(context);
                return null;
            });

        var handler = CreateHandler();
        var query = new SearchCustomerOrderQuery { CultureName = "en-US" };

        await handler.Handle(query, CancellationToken.None);

        capturedContexts.Should().HaveCount(2);
        capturedContexts[0].Should().BeSameAs(capturedContexts[1]);
        capturedContexts[0].CultureName.Should().Be("en-US");
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_MultipleAggregations_AssignsOrderByPosition()
    {
        var aggregations = new List<OrderAggregation>
        {
            new() { AggregationType = "attr", Field = "status" },
            new() { AggregationType = "attr", Field = "paymentMethod" },
        };

        _searchServiceMock
            .Setup(x => x.SearchCustomerOrdersAsync(It.IsAny<CustomerOrderIndexedSearchCriteria>()))
            .ReturnsAsync(new CustomerOrderIndexedSearchResult
            {
                TotalCount = 0,
                Results = [],
                Aggregations = aggregations,
            });

        _aggregateRepositoryMock
            .Setup(x => x.GetAggregatesFromOrdersAsync(It.IsAny<IList<CustomerOrder>>(), It.IsAny<string>()))
            .ReturnsAsync([]);

        _mapperMock
            .Setup(x => x.ToFacetResult(It.IsAny<OrderAggregation>(), It.IsAny<FacetMappingContext>()))
            .Returns<OrderAggregation, FacetMappingContext>((agg, _) => new TermFacetResult { Name = agg.Field });

        var handler = CreateHandler();
        var query = new SearchCustomerOrderQuery { CultureName = "en-US" };

        var response = await handler.Handle(query, CancellationToken.None);

        response.Facets.Should().HaveCount(2);
        response.Facets[0].Order.Should().Be(0);
        response.Facets[1].Order.Should().Be(1);
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_NoAggregations_ReturnsEmptyFacets()
    {
        _searchServiceMock
            .Setup(x => x.SearchCustomerOrdersAsync(It.IsAny<CustomerOrderIndexedSearchCriteria>()))
            .ReturnsAsync(new CustomerOrderIndexedSearchResult
            {
                TotalCount = 0,
                Results = [],
                Aggregations = null,
            });

        _aggregateRepositoryMock
            .Setup(x => x.GetAggregatesFromOrdersAsync(It.IsAny<IList<CustomerOrder>>(), It.IsAny<string>()))
            .ReturnsAsync([]);

        var handler = CreateHandler();
        var query = new SearchCustomerOrderQuery { CultureName = "en-US" };

        var response = await handler.Handle(query, CancellationToken.None);

        response.Facets.Should().BeEmpty();
    }
}
