using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.OrdersModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.Common;
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

        var result = AbstractTypeFactory<AggregationFacetSource>.TryCreateInstance();

        result.AggregationType = source.AggregationType;
        result.Field = source.Field;
        result.Labels = source.Labels?.Select(ToAggregationFacetLabel).ToList();
        result.Items = source.Items?.Select(ToAggregationFacetItem).ToList();

        return result;
    }

    protected virtual AggregationFacetItem ToAggregationFacetItem(OrderAggregationItem source)
    {
        var result = AbstractTypeFactory<AggregationFacetItem>.TryCreateInstance();

        result.Value = source.Value;
        result.Count = source.Count;
        result.IsApplied = source.IsApplied;
        result.Labels = source.Labels?.Select(ToAggregationFacetLabel).ToList();
        result.RequestedLowerBound = source.RequestedLowerBound;
        result.RequestedUpperBound = source.RequestedUpperBound;
        result.IncludeLower = source.IncludeLower;
        result.IncludeUpper = source.IncludeUpper;

        return result;
    }

    protected virtual AggregationFacetLabel ToAggregationFacetLabel(OrderAggregationLabel source)
    {
        var result = AbstractTypeFactory<AggregationFacetLabel>.TryCreateInstance();

        result.Language = source.Language;
        result.Label = source.Label;

        return result;
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
