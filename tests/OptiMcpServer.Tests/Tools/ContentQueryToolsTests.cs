using EPiServer.Core;
using EPiServer.DataAbstraction;
using Moq;
using OptiMcpServer.Models;
using OptiMcpServer.Tools;

namespace OptiMcpServer.Tests.Tools;

public class ContentQueryToolsTests
{
    // ── ParseContentReference ────────────────────────────────────────────────

    [Theory]
    [InlineData("5", 5, 0)]
    [InlineData("100", 100, 0)]
    [InlineData("1", 1, 0)]
    public void ParseContentReference_IntegerString_ReturnsCorrectId(string input, int expectedId, int expectedWorkId)
    {
        var result = ContentQueryTools.ParseContentReference(input);

        Assert.Equal(expectedId, result.ID);
        Assert.Equal(expectedWorkId, result.WorkID);
    }

    [Theory]
    [InlineData("5_3", 5, 3)]
    [InlineData("10_2", 10, 2)]
    public void ParseContentReference_IdWithWorkId_ReturnsCorrectIdAndWorkId(string input, int expectedId, int expectedWorkId)
    {
        var result = ContentQueryTools.ParseContentReference(input);

        Assert.Equal(expectedId, result.ID);
        Assert.Equal(expectedWorkId, result.WorkID);
    }

    [Fact]
    public void ParseContentReference_InvalidInput_ThrowsException()
    {
        Assert.Throws<FormatException>(() => ContentQueryTools.ParseContentReference("not-a-ref"));
    }

    // ── MapToDto ─────────────────────────────────────────────────────────────

    [Fact]
    public void MapToDto_BasicContent_MapsIdAndName()
    {
        var (content, typeRepo) = CreateMockContent(id: 42, name: "Test Page");

        var dto = ContentQueryTools.MapToDto(content.Object, typeRepo.Object);

        Assert.Equal("42", dto.Id);
        Assert.Equal("Test Page", dto.Name);
    }

    [Fact]
    public void MapToDto_WithContentType_MapsContentTypeName()
    {
        var contentType = new ContentType { ID = 7, Name = "StandardPage" };
        var (content, typeRepo) = CreateMockContent(id: 1, name: "Page", contentTypeId: 7);
        typeRepo.Setup(r => r.Load(7)).Returns(contentType);

        var dto = ContentQueryTools.MapToDto(content.Object, typeRepo.Object);

        Assert.Equal("StandardPage", dto.ContentTypeName);
        Assert.Equal(7, dto.ContentTypeId);
    }

    [Fact]
    public void MapToDto_WithParent_MapsParentId()
    {
        var (content, typeRepo) = CreateMockContent(id: 10, name: "Child", parentId: 5);

        var dto = ContentQueryTools.MapToDto(content.Object, typeRepo.Object);

        Assert.Equal("5", dto.ParentId);
    }

    [Fact]
    public void MapToDto_SkipsEpPrefixedProperties()
    {
        var (content, typeRepo) = CreateMockContent(id: 1, name: "Page");

        var propCollection = new PropertyDataCollection();
        var normalProp = new PropertyString { Name = "MainBody", Value = "hello" };
        var epProp = new PropertyString { Name = "EPPageType", Value = "should be skipped" };
        propCollection.Add(normalProp);
        propCollection.Add(epProp);
        content.SetupGet(c => c.Property).Returns(propCollection);

        var dto = ContentQueryTools.MapToDto(content.Object, typeRepo.Object);

        Assert.True(dto.Properties.ContainsKey("MainBody"));
        Assert.False(dto.Properties.Keys.Any(k => k.StartsWith("ep", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void MapToDto_WithProperties_MapsPropertyValues()
    {
        var (content, typeRepo) = CreateMockContent(id: 1, name: "Page");

        var propCollection = new PropertyDataCollection();
        propCollection.Add(new PropertyString { Name = "Title", Value = "Hello World" });
        content.SetupGet(c => c.Property).Returns(propCollection);

        var dto = ContentQueryTools.MapToDto(content.Object, typeRepo.Object);

        Assert.Equal("Hello World", dto.Properties["Title"]);
    }

    [Fact]
    public void MapToDto_WhenContentTypeNotFound_ContentTypeNameIsEmpty()
    {
        var (content, typeRepo) = CreateMockContent(id: 1, name: "Page", contentTypeId: 99);
        typeRepo.Setup(r => r.Load(99)).Returns((ContentType?)null);

        var dto = ContentQueryTools.MapToDto(content.Object, typeRepo.Object);

        Assert.Equal(string.Empty, dto.ContentTypeName);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (Mock<IContent> content, Mock<IContentTypeRepository> typeRepo) CreateMockContent(
        int id = 1,
        string name = "Test",
        int contentTypeId = 1,
        int parentId = 0)
    {
        var contentRef = new ContentReference(id);
        var parentRef = parentId > 0 ? new ContentReference(parentId) : ContentReference.EmptyReference;

        var contentType = new ContentType { ID = contentTypeId, Name = "TestType" };
        var typeRepo = new Mock<IContentTypeRepository>();
        typeRepo.Setup(r => r.Load(contentTypeId)).Returns(contentType);

        var content = new Mock<IContent>();
        content.SetupGet(c => c.ContentLink).Returns(contentRef);
        content.SetupGet(c => c.Name).Returns(name);
        content.SetupGet(c => c.ContentTypeID).Returns(contentTypeId);
        content.SetupGet(c => c.ParentLink).Returns(parentRef);
        content.SetupGet(c => c.Property).Returns(new PropertyDataCollection());

        return (content, typeRepo);
    }
}
