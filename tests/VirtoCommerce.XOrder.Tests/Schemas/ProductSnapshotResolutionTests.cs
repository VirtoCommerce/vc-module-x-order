using System.Collections.Generic;
using FluentAssertions;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using GraphQLParser.AST;
using MediatR;
using Moq;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Serialization;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XOrder.Core.Extensions;
using Xunit;

namespace VirtoCommerce.XOrder.Tests.Schemas;

public class ProductSnapshotResolutionTests
{
    [Fact]
    public void LoadOrderProductWithSnapshot_WithSnapshot_ReturnsDeserializedProduct()
    {
        // Arrange
        var product = new CatalogProduct { Id = "product1", Name = "Test Product" };
        var snapshotJson = ProductJsonSerializer.Serialize(product);

        var dataLoader = new Mock<IDataLoaderContextAccessor>();
        var context = CreateResolveFieldContext();
        var mediator = new Mock<IMediator>();

        // Act
        var result = dataLoader.Object.LoadOrderProductWithSnapshot(
            context, mediator.Object, "test_loader", "product1", snapshotJson);

        // Assert
        var expProduct = GetResultValue(result);
        expProduct.Should().NotBeNull();
        expProduct.IndexedProduct.Should().NotBeNull();
        expProduct.IndexedProduct.Id.Should().Be("product1");
        expProduct.IndexedProduct.Name.Should().Be("Test Product");
    }

    [Fact]
    public void LoadOrderProductWithSnapshot_WithSnapshot_CachesByProductId()
    {
        // Arrange
        var product = new CatalogProduct { Id = "product1", Name = "Test Product" };
        var snapshotJson = ProductJsonSerializer.Serialize(product);

        var dataLoader = new Mock<IDataLoaderContextAccessor>();
        var context = CreateResolveFieldContext();
        var mediator = new Mock<IMediator>();

        // Act — call twice with same ProductId
        var result1 = dataLoader.Object.LoadOrderProductWithSnapshot(
            context, mediator.Object, "test_loader", "product1", snapshotJson);
        var result2 = dataLoader.Object.LoadOrderProductWithSnapshot(
            context, mediator.Object, "test_loader", "product1", snapshotJson);

        // Assert — same instance returned (cached)
        var expProduct1 = GetResultValue(result1);
        var expProduct2 = GetResultValue(result2);
        expProduct1.Should().BeSameAs(expProduct2);
    }

    [Fact]
    public void LoadOrderProductWithSnapshot_WithNullProductId_ReturnsNullProduct()
    {
        // Arrange
        var dataLoader = new Mock<IDataLoaderContextAccessor>();
        var context = CreateResolveFieldContext();
        var mediator = new Mock<IMediator>();

        // Act
        var result = dataLoader.Object.LoadOrderProductWithSnapshot(
            context, mediator.Object, "test_loader", null, "some json");

        // Assert
        var expProduct = GetResultValue(result);
        expProduct.Should().BeNull();
    }

    private static IResolveFieldContext CreateResolveFieldContext()
    {
        var context = new Mock<IResolveFieldContext>();
        context.Setup(x => x.UserContext).Returns(new Dictionary<string, object>());
        context.Setup(x => x.SubFields).Returns(new Dictionary<string, (GraphQLField, FieldType)>());
        return context.Object;
    }

    private static ExpProduct GetResultValue(IDataLoaderResult<ExpProduct> result)
    {
        return result.GetResultAsync().GetAwaiter().GetResult();
    }
}
