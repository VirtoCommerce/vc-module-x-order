using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQLParser.AST;
using Moq;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XOrder.Core;
using VirtoCommerce.XOrder.Core.Extensions;
using VirtoCommerce.XOrder.Core.Models;
using VirtoCommerce.XOrder.Core.Services;
using Xunit;

namespace VirtoCommerce.XOrder.Tests.Schemas;

public class ProductSnapshotResolutionTests
{
    [Fact]
    public async Task LoadOrderProductWithSnapshot_WithSnapshot_ReturnsProduct()
    {
        // Arrange
        var product = new ExpProduct { IndexedProduct = new CatalogProduct { Id = "product1", Name = "Test Product" } };

        var dataLoader = new Mock<IDataLoaderContextAccessor>();
        dataLoader.SetupGet(x => x.Context).Returns(new DataLoaderContext());
        var context = CreateResolveFieldContext("order1", "lineItem1", products: [product]);

        // Act
        var result = dataLoader.Object.LoadOrderProductWithSnapshot(context, "test_loader", "product1");

        // Assert
        var expProduct = await GetResultValueAsync(result);
        expProduct.Should().NotBeNull();
        expProduct.IndexedProduct.Should().NotBeNull();
        expProduct.IndexedProduct.Id.Should().Be("product1");
        expProduct.IndexedProduct.Name.Should().Be("Test Product");
    }

    [Fact]
    public async Task LoadOrderProductWithSnapshot_DifferentOrders_ReturnsDifferentSnapshots()
    {
        // Arrange
        var product1 = new ExpProduct { IndexedProduct = new CatalogProduct { Id = "product1", Name = "Version A" } };
        var product2 = new ExpProduct { IndexedProduct = new CatalogProduct { Id = "product1", Name = "Version B" } };

        var dataLoader = new Mock<IDataLoaderContextAccessor>();
        dataLoader.SetupGet(x => x.Context).Returns(new DataLoaderContext());
        var userContext = new Dictionary<string, object>();
        var context1 = CreateResolveFieldContext("order1", "lineItem1", userContext, [product1]);
        var context2 = CreateResolveFieldContext("order2", "lineItem2", userContext, [product2]);

        // Act
        var result1 = dataLoader.Object.LoadOrderProductWithSnapshot(context1, "test_loader_order1", "product1");
        var result2 = dataLoader.Object.LoadOrderProductWithSnapshot(context2, "test_loader_order2", "product1");

        // Assert — different snapshots for different orders
        var expProduct1 = await GetResultValueAsync(result1);
        var expProduct2 = await GetResultValueAsync(result2);
        expProduct1.IndexedProduct.Name.Should().Be("Version A");
        expProduct2.IndexedProduct.Name.Should().Be("Version B");
    }

    [Fact]
    public async Task LoadOrderProductWithSnapshot_WithNullProductId_ReturnsNullProduct()
    {
        // Arrange
        var dataLoader = new Mock<IDataLoaderContextAccessor>();
        var context = CreateResolveFieldContext("order1", "lineItem1");

        // Act
        var result = dataLoader.Object.LoadOrderProductWithSnapshot(context, "test_loader", null);

        // Assert
        var expProduct = await GetResultValueAsync(result);
        expProduct.Should().BeNull();
    }

    private static IResolveFieldContext CreateResolveFieldContext(
        string orderId,
        string lineItemId,
        Dictionary<string, object> userContext = null,
        IList<ExpProduct> products = null)
    {
        userContext ??= [];

        var order = new CustomerOrder { Id = orderId };
        var orderAggregate = new CustomerOrderAggregate(null, null);
        orderAggregate.GrabCustomerOrder(order, null, null);

        // SetExpandedObjectGraph puts the aggregate under each entity's Id
        userContext.TryAdd(lineItemId, orderAggregate);
        userContext.TryAdd(orderId, orderAggregate);

        var lineItem = new LineItem { Id = lineItemId };

        var arguments = new Dictionary<string, ArgumentValue>
        {
            ["userId"] = new("testUser", ArgumentSource.Literal),
            ["cultureName"] = new("en-US", ArgumentSource.Literal),
        };

        var context = new Mock<IResolveFieldContext>();
        context.Setup(x => x.Source).Returns(lineItem);
        context.Setup(x => x.UserContext).Returns(userContext);
        context.Setup(x => x.Arguments).Returns(arguments);
        context.Setup(x => x.SubFields).Returns(new Dictionary<string, (GraphQLField, FieldType)>());

        // Mock IOrderProductResolver
        var resolver = new Mock<IOrderProductResolver>();
        resolver
            .Setup(x => x.ResolveOrderProductsAsync(
                orderId,
                It.IsAny<IList<string>>(),
                It.IsAny<OrderProductResolveContext>()))
            .ReturnsAsync((string _, IList<string> ids, OrderProductResolveContext _) =>
            {
                var result = new Dictionary<string, ExpProduct>();
                foreach (var product in products ?? [])
                {
                    if (ids.Contains(product.Id))
                    {
                        result[product.Id] = product;
                    }
                }

                return result;
            });

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IOrderProductResolver))).Returns(resolver.Object);

        context.Setup(x => x.RequestServices).Returns(serviceProvider.Object);

        return context.Object;
    }

    private static async Task<ExpProduct> GetResultValueAsync(IDataLoaderResult<ExpProduct> result)
    {
        return await result.GetResultAsync();
    }
}
