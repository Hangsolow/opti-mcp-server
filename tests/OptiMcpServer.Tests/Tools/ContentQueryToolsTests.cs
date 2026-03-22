using EPiServer.Core;
using EPiServer.DataAbstraction;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
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
        var (content, typeRepo) = CreateSubstituteContent(id: 42, name: "Test Page");

        var dto = ContentQueryTools.MapToDto(content, typeRepo);

        Assert.Equal("42", dto.Id);
        Assert.Equal("Test Page", dto.Name);
    }

    [Fact]
    public void MapToDto_WithContentType_MapsContentTypeName()
    {
        var (content, typeRepo) = CreateSubstituteContent(id: 1, name: "Page", contentTypeId: 7);
        var contentType = new ContentType { ID = 7, Name = "StandardPage" };
        typeRepo.Load(7).Returns(contentType);

        var dto = ContentQueryTools.MapToDto(content, typeRepo);

        Assert.Equal("StandardPage", dto.ContentTypeName);
        Assert.Equal(7, dto.ContentTypeId);
    }

    [Fact]
    public void MapToDto_WithParent_MapsParentId()
    {
        var (content, typeRepo) = CreateSubstituteContent(id: 10, name: "Child", parentId: 5);

        var dto = ContentQueryTools.MapToDto(content, typeRepo);

        Assert.Equal("5", dto.ParentId);
    }

    [Fact]
    public void MapToDto_SkipsEpPrefixedProperties()
    {
        var (content, typeRepo) = CreateSubstituteContent(id: 1, name: "Page");

        var propCollection = new PropertyDataCollection();
        propCollection.Add(new PropertyString { Name = "MainBody", Value = "hello" });
        propCollection.Add(new PropertyString { Name = "EPPageType", Value = "should be skipped" });
        content.Property.Returns(propCollection);

        var dto = ContentQueryTools.MapToDto(content, typeRepo);

        Assert.True(dto.Properties.ContainsKey("MainBody"));
        Assert.False(dto.Properties.Keys.Any(k => k.StartsWith("ep", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void MapToDto_WithProperties_MapsPropertyValues()
    {
        var (content, typeRepo) = CreateSubstituteContent(id: 1, name: "Page");

        var propCollection = new PropertyDataCollection();
        propCollection.Add(new PropertyString { Name = "Title", Value = "Hello World" });
        content.Property.Returns(propCollection);

        var dto = ContentQueryTools.MapToDto(content, typeRepo);

        Assert.Equal("Hello World", dto.Properties["Title"]);
    }

    [Fact]
    public void MapToDto_WhenContentTypeNotFound_ContentTypeNameIsEmpty()
    {
        var (content, typeRepo) = CreateSubstituteContent(id: 1, name: "Page", contentTypeId: 99);
        typeRepo.Load(99).ReturnsNull();

        var dto = ContentQueryTools.MapToDto(content, typeRepo);

        Assert.Equal(string.Empty, dto.ContentTypeName);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (IContent content, IContentTypeRepository typeRepo) CreateSubstituteContent(
        int id = 1,
        string name = "Test",
        int contentTypeId = 1,
        int parentId = 0)
    {
        var contentRef = new ContentReference(id);
        var parentRef = parentId > 0 ? new ContentReference(parentId) : ContentReference.EmptyReference;

        var contentType = new ContentType { ID = contentTypeId, Name = "TestType" };
        var typeRepo = Substitute.For<IContentTypeRepository>();
        typeRepo.Load(contentTypeId).Returns(contentType);

        var content = Substitute.For<IContent>();
        content.ContentLink.Returns(contentRef);
        content.Name.Returns(name);
        content.ContentTypeID.Returns(contentTypeId);
        content.ParentLink.Returns(parentRef);
        content.Property.Returns(new PropertyDataCollection());

        return (content, typeRepo);
    }
}
