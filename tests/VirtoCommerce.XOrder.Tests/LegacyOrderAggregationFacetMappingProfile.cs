using System;
using System.Linq;
using AutoMapper;
using VirtoCommerce.OrdersModule.Core.Model.Search;
using VirtoCommerce.Xapi.Core.Extensions;
using CoreFacets = VirtoCommerce.Xapi.Core.Models.Facets;

namespace VirtoCommerce.XOrder.Tests;

public class LegacyOrderAggregationFacetMappingProfile : Profile
{
    public LegacyOrderAggregationFacetMappingProfile()
    {
        CreateMap<OrderAggregation, CoreFacets.FacetResult>().IncludeAllDerived().ConvertUsing((request, facet, context) =>
        {
            context.Items.TryGetValue("cultureName", out var cultureNameObj);
            var cultureName = cultureNameObj as string;
            CoreFacets.FacetResult result = request.AggregationType switch
            {
                "attr" => new CoreFacets.TermFacetResult
                {
                    Name = request.Field,
                    Label = request.Field,
                    Terms = request.Items?.Select(x => new CoreFacets.FacetTerm
                    {
                        Count = x.Count,
                        IsSelected = x.IsApplied,
                        Term = x.Value?.ToString(),
                        Label = x.Labels?.FirstBestMatchForLanguage(x => x.Language, cultureName)?.Label ?? x.Value.ToString(),
                    }).ToArray() ?? [],

                },
                "range" => new CoreFacets.RangeFacetResult
                {
                    Name = request.Field,
                    Label = request.Field,
                    Ranges = request.Items?.Select(x => new CoreFacets.FacetRange
                    {
                        Count = x.Count,
                        IsSelected = x.IsApplied,
                        From = Convert.ToInt64(x.RequestedLowerBound),
                        IncludeFrom = x.IncludeLower,
                        FromStr = x.RequestedLowerBound,
                        To = Convert.ToInt64(x.RequestedUpperBound),
                        IncludeTo = x.IncludeUpper,
                        ToStr = x.RequestedUpperBound,
                        Label = x.Value?.ToString(),
                    }).ToArray() ?? [],
                },
                _ => null
            };

            return result;
        });
    }
}
