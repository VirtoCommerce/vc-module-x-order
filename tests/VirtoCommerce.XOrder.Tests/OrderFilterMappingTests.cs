using System;
using System.Collections.Generic;
using FluentAssertions;
using VirtoCommerce.OrdersModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.XOrder.Data.Services;
using Xunit;

namespace VirtoCommerce.XOrder.Tests;

public class OrderFilterMappingTests
{
    private readonly IXOrderMapper _mapper = new XOrderMapper();

    [Fact]
    public void MapTo_SetsMatchingFieldsOnPaymentSearchCriteria()
    {
        var filters = new List<IFilter>
        {
            new TermFilter { FieldName = "CustomerId", Values = ["customer-1"] },
        };

        var criteria = new PaymentSearchCriteria();

        _mapper.MapTo(filters, criteria);

        criteria.CustomerId.Should().Be("customer-1");
    }

    [Fact]
    public void MapTo_UnknownFieldName_DoesNotThrow()
    {
        var filters = new List<IFilter>
        {
            new TermFilter { FieldName = "NotAProperty", Values = ["value"] },
        };

        var criteria = new PaymentSearchCriteria();

        _mapper.MapTo(filters, criteria);

        criteria.CustomerId.Should().BeNull();
    }

    [Fact]
    public void MapTo_NullFilters_DoesNotThrow()
    {
        var criteria = new PaymentSearchCriteria();

        FluentActions.Invoking(() => _mapper.MapTo(null, criteria)).Should().NotThrow();
    }

    [Fact]
    public void MapTo_NullCriteria_Throws()
    {
        FluentActions.Invoking(() => _mapper.MapTo([], null)).Should().Throw<ArgumentNullException>();
    }
}
