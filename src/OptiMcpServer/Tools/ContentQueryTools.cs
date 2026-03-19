using System.ComponentModel;
using System.Text.Json;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using ModelContextProtocol.Server;
using OptiMcpServer.Models;

namespace OptiMcpServer.Tools;

[McpServerToolType]
public static class ContentQueryTools
{
    [McpServerTool, Description("Get a single content item by its ContentReference ID. Returns all properties as key-value pairs.")]
    public static string GetContent(
        IContentLoader contentLoader,
        IContentTypeRepository contentTypeRepository,
        [Description("The content ID, e.g. '5' or '5_3' (ID_WorkID)")] string id,
        [Description("Language branch, e.g. 'en' or 'sv'. Defaults to master language.")] string? language = null)
    {
        try
        {
            var contentRef = ParseContentReference(id);
            IContent content;

            if (language is not null)
            {
                var culture = new System.Globalization.CultureInfo(language);
                content = contentLoader.Get<IContent>(contentRef, culture);
            }
            else
            {
                content = contentLoader.Get<IContent>(contentRef);
            }

            var dto = MapToDto(content, contentTypeRepository);
            return JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (ContentNotFoundException)
        {
            return JsonSerializer.Serialize(new { error = $"Content with ID '{id}' not found." });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("List the immediate children of a content node.")]
    public static string GetChildren(
        IContentLoader contentLoader,
        IContentTypeRepository contentTypeRepository,
        [Description("The parent content ID, e.g. '5'")] string parentId,
        [Description("Language branch, e.g. 'en'. Defaults to master language.")] string? language = null,
        [Description("Maximum number of items to return (default 50)")] int pageSize = 50,
        [Description("Zero-based page index for pagination (default 0)")] int pageIndex = 0)
    {
        try
        {
            var parentRef = ParseContentReference(parentId);
            IEnumerable<IContent> children;

            if (language is not null)
            {
                var culture = new System.Globalization.CultureInfo(language);
                children = contentLoader.GetChildren<IContent>(parentRef, culture);
            }
            else
            {
                children = contentLoader.GetChildren<IContent>(parentRef);
            }

            var page = children.Skip(pageIndex * pageSize).Take(pageSize).ToList();
            var dtos = page.Select(c => MapToDto(c, contentTypeRepository)).ToList();

            return JsonSerializer.Serialize(new
            {
                parentId,
                pageIndex,
                pageSize,
                count = dtos.Count,
                items = dtos
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("Get the ancestor hierarchy (breadcrumb) for a content item, from the item up to the root.")]
    public static string GetAncestors(
        IContentLoader contentLoader,
        IContentTypeRepository contentTypeRepository,
        [Description("The content ID")] string id)
    {
        try
        {
            var contentRef = ParseContentReference(id);
            var ancestors = contentLoader.GetAncestors(contentRef);
            var dtos = ancestors.Select(c => MapToDto(c, contentTypeRepository)).ToList();

            return JsonSerializer.Serialize(new
            {
                contentId = id,
                ancestors = dtos
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    internal static ContentReference ParseContentReference(string id)
    {
        if (int.TryParse(id, out var intId))
            return new ContentReference(intId);

        return ContentReference.Parse(id);
    }

    internal static ContentDto MapToDto(IContent content, IContentTypeRepository contentTypeRepository)
    {
        var contentType = contentTypeRepository.Load(content.ContentTypeID);

        var properties = new Dictionary<string, string?>();
        foreach (PropertyData prop in content.Property)
        {
            if (prop.Name.StartsWith("ep", StringComparison.OrdinalIgnoreCase))
                continue; // skip internal EPiServer system properties

            properties[prop.Name] = prop.Value?.ToString();
        }

        string status = "Unknown";
        if (content is IVersionable versionable)
        {
            status = versionable.Status.ToString();
        }

        return new ContentDto
        {
            Id = content.ContentLink.ToString(),
            Name = content.Name,
            ContentTypeId = content.ContentTypeID,
            ContentTypeName = contentType?.Name ?? string.Empty,
            ParentId = content.ParentLink?.ToString() ?? string.Empty,
            Language = content is ILocalizable localizable ? localizable.Language?.Name ?? string.Empty : string.Empty,
            Status = status,
            Created = content is IChangeTrackable trackable ? trackable.Created : null,
            Changed = content is IChangeTrackable trackable2 ? trackable2.Changed : null,
            Properties = properties
        };
    }
}
