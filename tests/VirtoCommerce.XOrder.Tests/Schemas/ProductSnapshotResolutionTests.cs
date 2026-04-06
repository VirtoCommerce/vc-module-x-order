using System;
using System.Collections.Generic;
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
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.ProductSnapshot.Core.Services;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XOrder.Core;
using VirtoCommerce.XOrder.Core.Extensions;
using Xunit;

namespace VirtoCommerce.XOrder.Tests.Schemas;

public class ProductSnapshotResolutionTests
{
    [Fact]
    public async Task LoadOrderProduct_WithSnapshot_ReturnsProduct()
    {
        // Arrange
        var product = new CatalogProduct { Id = "product1", Name = "Test Product" };

        var dataLoader = new Mock<IDataLoaderContextAccessor>();
        dataLoader.SetupGet(x => x.Context).Returns(new DataLoaderContext());
        var context = CreateResolveFieldContext("order1", "lineItem1", products: [product]);
        var mediator = new Mock<IMediator>();

        // Act
        var result = dataLoader.Object.LoadOrderProduct(context, mediator.Object, "test_loader", "product1");

        // Assert
        var expProduct = await GetResultValueAsync(result);
        expProduct.Should().NotBeNull();
        expProduct.IndexedProduct.Should().NotBeNull();
        expProduct.IndexedProduct.Id.Should().Be("product1");
        expProduct.IndexedProduct.Name.Should().Be("Test Product");
    }

    [Fact]
    public async Task LoadOrderProduct_DifferentOrders_ReturnsDifferentSnapshots()
    {
        // Arrange
        var product1 = new CatalogProduct { Id = "product1", Name = "Version A" };
        var product2 = new CatalogProduct { Id = "product1", Name = "Version B" };

        var dataLoader = new Mock<IDataLoaderContextAccessor>();
        dataLoader.SetupGet(x => x.Context).Returns(new DataLoaderContext());
        var userContext = new Dictionary<string, object>();
        var context1 = CreateResolveFieldContext("order1", "lineItem1", userContext, [product1]);
        var context2 = CreateResolveFieldContext("order2", "lineItem2", userContext, [product2]);
        var mediator = new Mock<IMediator>();

        // Act
        var result1 = dataLoader.Object.LoadOrderProduct(context1, mediator.Object, "test_loader", "product1");
        var result2 = dataLoader.Object.LoadOrderProduct(context2, mediator.Object, "test_loader", "product1");

        // Assert — different snapshots for different orders
        var expProduct1 = await GetResultValueAsync(result1);
        var expProduct2 = await GetResultValueAsync(result2);
        expProduct1.IndexedProduct.Name.Should().Be("Version A");
        expProduct2.IndexedProduct.Name.Should().Be("Version B");
    }

    [Fact]
    public async Task LoadOrderProduct_WithNullProductId_ReturnsNullProduct()
    {
        // Arrange
        var dataLoader = new Mock<IDataLoaderContextAccessor>();
        var context = CreateResolveFieldContext("order1", "lineItem1");
        var mediator = new Mock<IMediator>();

        // Act
        var result = dataLoader.Object.LoadOrderProduct(context, mediator.Object, "test_loader", null);

        // Assert
        var expProduct = await GetResultValueAsync(result);
        expProduct.Should().BeNull();
    }

    private static IResolveFieldContext CreateResolveFieldContext(
        string orderId,
        string lineItemId,
        Dictionary<string, object> userContext = null,
        IList<CatalogProduct> products = null)
    {
        userContext ??= new Dictionary<string, object>();

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
        var moduleCatalog = new Mock<IModuleCatalog>();
        var moduleManifest = new ModuleManifest { Id = "VirtoCommerce.ProductSnapshot", Version = "1.0.0", VersionTag = "", PlatformVersion = "1.0.0" };
        var manifestModuleInfo = new ManifestModuleInfo { IsInstalled = true };
        manifestModuleInfo.LoadFromManifest(moduleManifest);
        moduleCatalog.SetupGet(x => x.Modules).Returns([manifestModuleInfo]);

        var snapshotProvider = new Mock<ICatalogProductSnapshotProvider>();
        snapshotProvider
            .Setup(x => x.GetOrderProductSnapshotsAsync(It.Is<string>(x => x.EqualsIgnoreCase(orderId))))
            .ReturnsAsync(products ?? []);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IModuleCatalog))).Returns(moduleCatalog.Object);
        serviceProvider.Setup(x => x.GetService(typeof(ICatalogProductSnapshotProvider))).Returns(snapshotProvider.Object);

        context.Setup(x => x.RequestServices).Returns(serviceProvider.Object);

        return context.Object;
    }

    private static async Task<ExpProduct> GetResultValueAsync(IDataLoaderResult<ExpProduct> result)
    {
        return await result.GetResultAsync();
    }
}
