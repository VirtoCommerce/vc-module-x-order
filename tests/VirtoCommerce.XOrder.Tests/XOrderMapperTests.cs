using System;
using System.Linq;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.OrdersModule.Core.Model.Search;
using VirtoCommerce.Xapi.Core.Models.Facets;
using VirtoCommerce.XOrder.Data.Extensions;
using VirtoCommerce.XOrder.Data.Services;
using Xunit;

namespace VirtoCommerce.XOrder.Tests;

public class XOrderMapperTests
{
    private static readonly IMapper _legacyMapper = new MapperConfiguration(cfg =>
        cfg.AddProfile<LegacyOrderAggregationFacetMappingProfile>()).CreateMapper();

    private readonly XOrderMapper _mapper = new();

    [Fact]
    public void ToFacetResult_NullSource_ReturnsNull()
    {
        var result = _mapper.ToFacetResult(null, new FacetMappingContext { CultureName = "en-US" });

        result.Should().BeNull();
    }

    [Fact]
    public void ToFacetResult_AttrAggregation_MapsToTermFacetResult()
    {
        var source = new OrderAggregation
        {
            AggregationType = "attr",
            Field = "status",
            Items =
            [
                new OrderAggregationItem
                {
                    Value = "new",
                    Count = 4,
                    IsApplied = true,
                    Labels = [new OrderAggregationLabel { Language = "en-US", Label = "New" }],
                },
            ],
        };

        var result = _mapper.ToFacetResult(source, new FacetMappingContext { CultureName = "en-US" }) as TermFacetResult;

        result.Should().NotBeNull();
        result.Name.Should().Be("status");
        result.Label.Should().Be("status");
        result.Terms.Should().HaveCount(1);
        result.Terms[0].Term.Should().Be("new");
        result.Terms[0].Label.Should().Be("New");
        result.Terms[0].Count.Should().Be(4);
        result.Terms[0].IsSelected.Should().BeTrue();
    }

    [Fact]
    public void ToFacetResult_RangeAggregation_MapsToRangeFacetResult()
    {
        var source = new OrderAggregation
        {
            AggregationType = "range",
            Field = "total",
            Items =
            [
                new OrderAggregationItem
                {
                    Value = "0-100",
                    Count = 2,
                    IsApplied = false,
                    RequestedLowerBound = "0",
                    RequestedUpperBound = "100",
                    IncludeLower = true,
                    IncludeUpper = false,
                },
            ],
        };

        var result = _mapper.ToFacetResult(source, new FacetMappingContext { CultureName = "en-US" }) as RangeFacetResult;

        result.Should().NotBeNull();
        result.Name.Should().Be("total");
        result.Label.Should().Be("total");
        result.Ranges.Should().HaveCount(1);
        result.Ranges[0].From.Should().Be(0);
        result.Ranges[0].To.Should().Be(100);
        result.Ranges[0].IncludeFrom.Should().BeTrue();
        result.Ranges[0].IncludeTo.Should().BeFalse();
        result.Ranges[0].Count.Should().Be(2);
    }

    [Fact]
    public void ToFacetResult_RangeAggregation_EmptyBounds_ThrowsFormatException()
    {
        var source = new OrderAggregation
        {
            AggregationType = "range",
            Field = "total",
            Items = [new OrderAggregationItem { RequestedLowerBound = "", RequestedUpperBound = "100" }],
        };

        var act = () => _mapper.ToFacetResult(source, new FacetMappingContext { CultureName = "en-US" });

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void ToFacetResult_UnrecognizedAggregationType_ReturnsNull()
    {
        var source = new OrderAggregation { AggregationType = "category", Field = "categoryId" };

        var result = _mapper.ToFacetResult(source, new FacetMappingContext { CultureName = "en-US" });

        result.Should().BeNull();
    }

    [Fact]
    public void ToFacetResult_ProducesSameResultAsLegacyAutoMapperConventionMap()
    {
        var source = new OrderAggregation
        {
            AggregationType = "attr",
            Field = "status",
            Items =
            [
                new OrderAggregationItem
                {
                    Value = "new",
                    Count = 4,
                    IsApplied = true,
                    Labels = [new OrderAggregationLabel { Language = "en-US", Label = "New" }],
                },
            ],
        };

        var expected = _legacyMapper.Map<FacetResult>(source, options => options.Items["cultureName"] = "en-US");
        var actual = _mapper.ToFacetResult(source, new FacetMappingContext { CultureName = "en-US" });

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ToFacetResult_RangeAggregation_ProducesSameResultAsLegacyAutoMapperConventionMap()
    {
        var source = new OrderAggregation
        {
            AggregationType = "range",
            Field = "total",
            Items =
            [
                new OrderAggregationItem
                {
                    Value = "0-100",
                    Count = 2,
                    IsApplied = false,
                    RequestedLowerBound = "0",
                    RequestedUpperBound = "100",
                    IncludeLower = true,
                    IncludeUpper = false,
                },
            ],
        };

        var expected = _legacyMapper.Map<FacetResult>(source, options => options.Items["cultureName"] = "en-US");
        var actual = _mapper.ToFacetResult(source, new FacetMappingContext { CultureName = "en-US" });

        actual.Should().BeEquivalentTo(expected);
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
