using System;
using System.Linq;
using VirtoCommerce.OrdersModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Models.Facets;

namespace VirtoCommerce.XOrder.Data.Services;

public class XOrderMapper : IXOrderMapper
{
    public virtual FacetResult ToFacetResult(OrderAggregation source, string cultureName)
    {
        if (source == null)
        {
            return null;
        }

        return source.AggregationType switch
        {
            "attr" => ToTermFacetResult(source, cultureName),
            "range" => ToRangeFacetResult(source),
            _ => null,
        };
    }

    protected virtual TermFacetResult ToTermFacetResult(OrderAggregation source, string cultureName)
    {
        var result = AbstractTypeFactory<TermFacetResult>.TryCreateInstance();

        result.Name = source.Field;
        result.Label = source.Field;
        result.Terms = source.Items?.Select(x => ToFacetTerm(x, cultureName)).ToArray() ?? [];

        return result;
    }

    protected virtual FacetTerm ToFacetTerm(OrderAggregationItem source, string cultureName)
    {
        var result = AbstractTypeFactory<FacetTerm>.TryCreateInstance();

        result.Count = source.Count;
        result.IsSelected = source.IsApplied;
        result.Term = source.Value?.ToString();
        result.Label = source.Labels?.FirstBestMatchForLanguage(x => x.Language, cultureName)?.Label ?? source.Value?.ToString();

        return result;
    }

    protected virtual RangeFacetResult ToRangeFacetResult(OrderAggregation source)
    {
        var result = AbstractTypeFactory<RangeFacetResult>.TryCreateInstance();

        result.Name = source.Field;
        result.Label = source.Field;
        result.Ranges = source.Items?.Select(ToFacetRange).ToArray() ?? [];

        return result;
    }

    protected virtual FacetRange ToFacetRange(OrderAggregationItem source)
    {
        var result = AbstractTypeFactory<FacetRange>.TryCreateInstance();

        result.Count = source.Count;
        result.IsSelected = source.IsApplied;
        result.From = Convert.ToInt64(source.RequestedLowerBound);
        result.IncludeFrom = source.IncludeLower;
        result.FromStr = source.RequestedLowerBound;
        result.To = Convert.ToInt64(source.RequestedUpperBound);
        result.IncludeTo = source.IncludeUpper;
        result.ToStr = source.RequestedUpperBound;
        result.Label = source.Value?.ToString();

        return result;
    }
}
