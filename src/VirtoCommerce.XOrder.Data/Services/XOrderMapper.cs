using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.OrdersModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.Xapi.Core.Index;
using VirtoCommerce.Xapi.Core.Models.Facets;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XOrder.Core.Services;

namespace VirtoCommerce.XOrder.Data.Services;

public class XOrderMapper : IXOrderMapper
{
    private readonly IFacetMapper _facetMapper;

    public XOrderMapper(IFacetMapper facetMapper)
    {
        _facetMapper = facetMapper;
    }

    public virtual FacetResult ToFacetResult(OrderAggregation source, FacetMappingContext context)
    {
        return _facetMapper.ToFacetResult(ToAggregationFacetSource(source), context);
    }

    protected virtual AggregationFacetSource ToAggregationFacetSource(OrderAggregation source)
    {
        if (source == null)
        {
            return null;
        }

        return new AggregationFacetSource
        {
            AggregationType = source.AggregationType,
            Field = source.Field,
            Labels = source.Labels?.Select(ToAggregationFacetLabel).ToList(),
            Items = source.Items?.Select(ToAggregationFacetItem).ToList(),
        };
    }

    protected virtual AggregationFacetItem ToAggregationFacetItem(OrderAggregationItem source)
    {
        return new AggregationFacetItem
        {
            Value = source.Value,
            Count = source.Count,
            IsApplied = source.IsApplied,
            Labels = source.Labels?.Select(ToAggregationFacetLabel).ToList(),
            RequestedLowerBound = source.RequestedLowerBound,
            RequestedUpperBound = source.RequestedUpperBound,
            IncludeLower = source.IncludeLower,
            IncludeUpper = source.IncludeUpper,
        };
    }

    protected virtual AggregationFacetLabel ToAggregationFacetLabel(OrderAggregationLabel source)
    {
        return new AggregationFacetLabel
        {
            Language = source.Language,
            Label = source.Label,
        };
    }

    public virtual void MapTo(IList<IFilter> filters, PaymentSearchCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        if (filters == null)
        {
            return;
        }

        foreach (var term in filters.OfType<TermFilter>())
        {
            term.MapTo(criteria);
        }
    }
}
