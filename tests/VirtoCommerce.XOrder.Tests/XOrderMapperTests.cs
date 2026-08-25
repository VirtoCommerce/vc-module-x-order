using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VirtoCommerce.OrdersModule.Core.Model.Search;
using VirtoCommerce.Xapi.Core.Models.Facets;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XOrder.Data.Extensions;
using VirtoCommerce.XOrder.Data.Services;
using Xunit;

namespace VirtoCommerce.XOrder.Tests;

public class XOrderMapperTests
{
    [Fact]
    public void ToFacetResult_NullSource_PassesNullToFacetMapperAndReturnsNull()
    {
        AggregationFacetSource captured = null;
        var facetMapperMock = new Mock<IFacetMapper>();
        facetMapperMock
            .Setup(x => x.ToFacetResult(It.IsAny<AggregationFacetSource>(), It.IsAny<FacetMappingContext>()))
            .Callback<AggregationFacetSource, FacetMappingContext>((source, _) => captured = source)
            .Returns((FacetResult)null);
        var mapper = new XOrderMapper(facetMapperMock.Object);

        var result = mapper.ToFacetResult(null, new FacetMappingContext { CultureName = "en-US" });

        captured.Should().BeNull();
        result.Should().BeNull();
    }

    [Fact]
    public void ToFacetResult_ConvertsOrderAggregationToAggregationFacetSource()
    {
        AggregationFacetSource captured = null;
        var facetMapperMock = new Mock<IFacetMapper>();
        facetMapperMock
            .Setup(x => x.ToFacetResult(It.IsAny<AggregationFacetSource>(), It.IsAny<FacetMappingContext>()))
            .Callback<AggregationFacetSource, FacetMappingContext>((source, _) => captured = source);
        var mapper = new XOrderMapper(facetMapperMock.Object);

        var source = new OrderAggregation
        {
            AggregationType = "attr",
            Field = "status",
            Labels = [new OrderAggregationLabel { Language = "en-US", Label = "Status" }],
            Items =
            [
                new OrderAggregationItem
                {
                    Value = "new",
                    Count = 4,
                    IsApplied = true,
                    Labels = [new OrderAggregationLabel { Language = "en-US", Label = "New" }],
                    RequestedLowerBound = "0",
                    RequestedUpperBound = "100",
                    IncludeLower = true,
                    IncludeUpper = false,
                },
            ],
        };

        mapper.ToFacetResult(source, new FacetMappingContext { CultureName = "en-US" });

        captured.Should().NotBeNull();
        captured!.AggregationType.Should().Be("attr");
        captured.Field.Should().Be("status");
        captured.Labels.Should().ContainSingle().Which.Label.Should().Be("Status");

        captured.Items.Should().ContainSingle();
        var item = captured.Items![0];
        item.Value.Should().Be("new");
        item.Count.Should().Be(4);
        item.IsApplied.Should().BeTrue();
        item.Labels.Should().ContainSingle().Which.Label.Should().Be("New");
        item.RequestedLowerBound.Should().Be("0");
        item.RequestedUpperBound.Should().Be("100");
        item.IncludeLower.Should().BeTrue();
        item.IncludeUpper.Should().BeFalse();

        // OrderAggregation has no Statistics/TermValuesSortingType concept - the DTO must reflect that.
        captured.Statistics.Should().BeNull();
        captured.TermValuesSortingType.Should().BeNull();
    }

    [Fact]
    public void ToFacetResult_NoAggregationLevelLabels_PassesNullLabelsThrough()
    {
        // Orders never sets Labels above item level - passing null through lets the shared mapper's
        // own fallback reproduce this module's old Label == Field behaviour.
        AggregationFacetSource captured = null;
        var facetMapperMock = new Mock<IFacetMapper>();
        facetMapperMock
            .Setup(x => x.ToFacetResult(It.IsAny<AggregationFacetSource>(), It.IsAny<FacetMappingContext>()))
            .Callback<AggregationFacetSource, FacetMappingContext>((source, _) => captured = source);
        var mapper = new XOrderMapper(facetMapperMock.Object);

        mapper.ToFacetResult(new OrderAggregation { AggregationType = "attr", Field = "status" }, new FacetMappingContext());

        captured!.Labels.Should().BeNull();
    }

    [Fact]
    public void ToFacetResult_ReturnsFacetMapperResult()
    {
        var expected = new TermFacetResult();
        var facetMapperMock = new Mock<IFacetMapper>();
        facetMapperMock
            .Setup(x => x.ToFacetResult(It.IsAny<AggregationFacetSource>(), It.IsAny<FacetMappingContext>()))
            .Returns(expected);
        var mapper = new XOrderMapper(facetMapperMock.Object);

        var result = mapper.ToFacetResult(new OrderAggregation { AggregationType = "attr" }, new FacetMappingContext());

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public void CreateFacetMappingContext_DelegatesToFacetMapper()
    {
        var expected = new FacetMappingContext();
        var facetMapperMock = new Mock<IFacetMapper>();
        facetMapperMock
            .Setup(x => x.CreateFacetMappingContext("en-US"))
            .Returns(expected);
        var mapper = new XOrderMapper(facetMapperMock.Object);

        var result = mapper.CreateFacetMappingContext("en-US");

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public void AddXOrder_Registers_XOrderMapper_AsSingleton()
    {
        var services = new ServiceCollection();
        var graphQlBuilder = new GraphQL.MicrosoftDI.GraphQLBuilder(services, _ => { });

        services.AddXOrder(graphQlBuilder);

        var descriptor = services.SingleOrDefault(x => x.ServiceType == typeof(IXOrderMapper));

        descriptor.Should().NotBeNull();
        descriptor.ImplementationType.Should().Be<XOrderMapper>();
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }
}
