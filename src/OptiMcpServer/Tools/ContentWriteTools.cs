using System.ComponentModel;
using System.Text.Json;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess;
using EPiServer.Security;
using ModelContextProtocol.Server;
using OptiMcpServer.Tools;

namespace OptiMcpServer.Tools;

[McpServerToolType]
public static class ContentWriteTools
{
    [McpServerTool, Description("Create a new content item under a parent. Returns the new ContentReference ID.")]
    public static string CreateContent(
        IContentRepository contentRepository,
        IContentTypeRepository contentTypeRepository,
        [Description("The content type name (e.g. 'StandardPage') or numeric content type ID")] string contentType,
        [Description("The parent content ID under which to create the new item")] string parentId,
        [Description("The name (title) of the new content item")] string name,
        [Description("Optional: JSON object of property name/value pairs to set, e.g. {\"MainBody\": \"<p>Hello</p>\"}")] string? propertiesJson = null)
    {
        try
        {
            var parentRef = ContentQueryTools.ParseContentReference(parentId);

            ContentType? resolvedType = int.TryParse(contentType, out var typeId)
                ? contentTypeRepository.Load(typeId)
                : contentTypeRepository.Load(contentType);

            if (resolvedType is null)
                return JsonSerializer.Serialize(new { error = $"Content type '{contentType}' not found." });

            var newContent = contentRepository.GetDefault<IContent>(parentRef, resolvedType.ID);
            newContent.Name = name;

            if (propertiesJson is not null)
            {
                var props = JsonSerializer.Deserialize<Dictionary<string, string>>(propertiesJson);
                ApplyProperties(newContent, props);
            }

            var savedRef = contentRepository.Save(newContent, SaveAction.Save | SaveAction.ForceNewVersion, AccessLevel.NoAccess);

            return JsonSerializer.Serialize(new
            {
                success = true,
                id = savedRef.ToString(),
                name,
                message = $"Content created with ID {savedRef}. Use publish_content to publish it."
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("Update properties on an existing content item. Creates a new draft version.")]
    public static string UpdateContent(
        IContentLoader contentLoader,
        IContentRepository contentRepository,
        [Description("The content ID to update")] string id,
        [Description("JSON object of property name/value pairs to update, e.g. {\"MainBody\": \"<p>Updated</p>\"}")] string propertiesJson,
        [Description("Language branch to update, e.g. 'en'")] string? language = null)
    {
        try
        {
            var contentRef = ContentQueryTools.ParseContentReference(id);
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

            var writable = (IContent)((EPiServer.Data.Entity.IReadOnly)content).CreateWritableClone();

            var props = JsonSerializer.Deserialize<Dictionary<string, string>>(propertiesJson);
            ApplyProperties(writable, props);

            var savedRef = contentRepository.Save(writable, SaveAction.Save | SaveAction.ForceNewVersion, AccessLevel.NoAccess);

            return JsonSerializer.Serialize(new
            {
                success = true,
                id = savedRef.ToString(),
                message = $"Content updated (new version: {savedRef}). Use publish_content to publish."
            });
        }
        catch (ContentNotFoundException)
        {
            return JsonSerializer.Serialize(new { error = $"Content '{id}' not found." });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("Publish a content item, making it live on the site.")]
    public static string PublishContent(
        IContentLoader contentLoader,
        IContentRepository contentRepository,
        [Description("The content ID to publish")] string id,
        [Description("Language branch to publish, e.g. 'en'")] string? language = null)
    {
        try
        {
            var contentRef = ContentQueryTools.ParseContentReference(id);
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

            var writable = (IContent)((EPiServer.Data.Entity.IReadOnly)content).CreateWritableClone();
            var savedRef = contentRepository.Save(writable, SaveAction.Publish | SaveAction.ForceCurrentVersion, AccessLevel.NoAccess);

            return JsonSerializer.Serialize(new
            {
                success = true,
                id = savedRef.ToString(),
                message = $"Content '{writable.Name}' published successfully."
            });
        }
        catch (ContentNotFoundException)
        {
            return JsonSerializer.Serialize(new { error = $"Content '{id}' not found." });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("Move a content item to the wastebasket (soft delete). It can be restored from the CMS admin UI.")]
    public static string MoveToTrash(
        IContentRepository contentRepository,
        [Description("The content ID to move to the wastebasket")] string id)
    {
        try
        {
            var contentRef = ContentQueryTools.ParseContentReference(id);
            contentRepository.MoveToWastebasket(contentRef);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Content '{id}' moved to wastebasket."
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private static void ApplyProperties(IContent content, Dictionary<string, string>? props)
    {
        if (props is null) return;

        foreach (var kvp in props)
        {
            var prop = content.Property[kvp.Key];
            if (prop is not null)
            {
                prop.ParseToSelf(kvp.Value);
            }
        }
    }
}
