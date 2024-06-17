using System;
using System.Collections.Generic;
using AutoMapper;
using VirtoCommerce.OrdersModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.XOrder.Data.Mapping;
using Xunit;

namespace VirtoCommerce.Xapi.XOrder.Tests
{
    public class MappingTermFilterTests
    {
        [Fact]
        public void OrderMappingProfileTest()
        {
            // Arrange
            var mapperCfg = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new OrderMappingProfile());
            });

            var mapper = mapperCfg.CreateMapper();
            var terms = new List<IFilter>
            {
                new TermFilter { FieldName = "CustomerId", Values = new[] { Guid.NewGuid().ToString() } },
                new TermFilter { FieldName = "CustomerIds", Values = new[] { Guid.NewGuid().ToString() } },
                new TermFilter { FieldName = "SubscriptionIds", Values = Array.Empty<string>() },
                new TermFilter { FieldName = "SubscriptionIds", Values = null }
            };

            // Action
            var criteria = new CustomerOrderSearchCriteria();
            mapper.Map(terms, criteria);

            // Assert
            Assert.NotNull(criteria);
            Assert.NotNull(criteria.CustomerId);
            Assert.NotNull(criteria.CustomerIds);
            Assert.NotNull(criteria.CustomerId);
            Assert.Null(criteria.SubscriptionIds);
        }
    }
}
