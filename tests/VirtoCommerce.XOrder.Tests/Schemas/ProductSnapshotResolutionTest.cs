using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using GraphQLParser.AST;
using MediatR;
using Moq;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XOrder.Core;
using VirtoCommerce.XOrder.Core.Extensions;
using VirtoCommerce.XOrder.Core.Models;
using Xunit;

namespace VirtoCommerce.XOrder.Tests.Schemas;

public class ProductSnapshotResolutionTests
{
    [Fact]
    public async Task LoadSnapshotProductsAsync_WithSnapshot_ReturnsProduct()
    {
        // Arrange
        var product = new ExpProduct { IndexedProduct = new CatalogProduct { Id = "product1", Name = "Test Product" } };

        var dataLoader = new Mock<IDataLoaderContextAccessor>();
        dataLoader.SetupGet(x => x.Context).Returns(new DataLoaderContext());
        var context = CreateResolveFieldContext("order1", "lineItem1", products: [product]);
        var mediator = new Mock<IMediator>();

        // Act
        var result = dataLoader.Object.LoadOrderProductWithSnapshot(context, mediator.Object, "test_loader", "product1");

        // Assert
        var expProduct = await GetResultValueAsync(result);
        expProduct.Should().NotBeNull();
        expProduct.IndexedProduct.Should().NotBeNull();
        expProduct.IndexedProduct.Id.Should().Be("product1");
        expProduct.IndexedProduct.Name.Should().Be("Test Product");
    }

    [Fact]
    public async Task LoadSnapshotProductsAsync_DifferentOrders_ReturnsDifferentSnapshots()
    {
        // Arrange
        var product1 = new ExpProduct { IndexedProduct = new CatalogProduct { Id = "product1", Name = "Version A" } };
        var product2 = new ExpProduct { IndexedProduct = new CatalogProduct { Id = "product1", Name = "Version B" } };

        var dataLoader = new Mock<IDataLoaderContextAccessor>();
        dataLoader.SetupGet(x => x.Context).Returns(new DataLoaderContext());
        var userContext = new Dictionary<string, object>();
        var context1 = CreateResolveFieldContext("order1", "lineItem1", userContext, [product1]);
        var context2 = CreateResolveFieldContext("order2", "lineItem2", userContext, [product2]);
        var mediator = new Mock<IMediator>();

        // Act
        var result1 = dataLoader.Object.LoadOrderProductWithSnapshot(context1, mediator.Object, "test_loader_order1", "product1");
        var result2 = dataLoader.Object.LoadOrderProductWithSnapshot(context2, mediator.Object, "test_loader_order2", "product1");

        // Assert — different snapshots for different orders
        var expProduct1 = await GetResultValueAsync(result1);
        var expProduct2 = await GetResultValueAsync(result2);
        expProduct1.IndexedProduct.Name.Should().Be("Version A");
        expProduct2.IndexedProduct.Name.Should().Be("Version B");
    }

    [Fact]
    public async Task LoadSnapshotProductsAsync_WithNullProductId_ReturnsNullProduct()
    {
        // Arrange
        var dataLoader = new Mock<IDataLoaderContextAccessor>();
        var context = CreateResolveFieldContext("order1", "lineItem1");
        var mediator = new Mock<IMediator>();

        // Act
        var result = dataLoader.Object.LoadOrderProductWithSnapshot(context, mediator.Object, "test_loader", null);

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

        var context = new Mock<IResolveFieldContext>();
        context.Setup(x => x.Source).Returns(lineItem);
        context.Setup(x => x.UserContext).Returns(userContext);
        context.Setup(x => x.SubFields).Returns(new Dictionary<string, (GraphQLField, FieldType)>());

        // mock service resolver
        var pipeleine = new Mock<IGenericPipelineLauncher>();
        pipeleine
            .Setup(x => x.Execute(It.Is<ExternalOrderProducts>(x => x.OrderId == orderId)))
            .Callback((ExternalOrderProducts x) =>
            {
                x.Products = !products.IsNullOrEmpty() ? products.ToDictionary(x => x.Id) : [];
            });

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IGenericPipelineLauncher))).Returns(pipeleine.Object);

        context.Setup(x => x.RequestServices).Returns(serviceProvider.Object);

        return context.Object;
    }

    private static async Task<ExpProduct> GetResultValueAsync(IDataLoaderResult<ExpProduct> result)
    {
        return await result.GetResultAsync();
    }
}
