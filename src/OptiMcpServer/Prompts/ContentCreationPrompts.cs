using System.ComponentModel;
using System.Text;
using EPiServer.DataAbstraction;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using OptiMcpServer.Options;

namespace OptiMcpServer.Prompts;

[McpServerPromptType]
public static class ContentCreationPrompts
{
    [McpServerPrompt, Description("Returns a prompt that guides an AI through creating a new page in Optimizely CMS. Lists available page types and the steps to create and publish the page.")]
    public static string CreatePage(
        IContentTypeRepository contentTypeRepository,
        IOptions<ContentCreationOptions> options,
        [Description("Name for the new page")] string name,
        [Description("Content ID of the parent page under which to create this page")] string parentId,
        [Description("Language branch, e.g. 'en'. Omit to use the master language.")] string? language = null,
        [Description("Content type name to use. If omitted, the prompt will ask the AI to choose.")] string? pageType = null)
    {
        var baseClassName = options.Value.PageBaseClass;
        var baseType = ResolveType(baseClassName);
        var types = GetFilteredContentTypes(contentTypeRepository, baseType);

        var sb = new StringBuilder();
        sb.AppendLine($"Create a new page in Optimizely CMS with the following details:");
        sb.AppendLine($"- Name: {name}");
        sb.AppendLine($"- Parent content ID: {parentId}");
        sb.AppendLine($"- Language: {language ?? "master language"}");
        sb.AppendLine();

        sb.AppendLine($"Available page types (inheriting from {baseClassName}):");
        if (types.Count == 0)
        {
            sb.AppendLine("  (No page types found. Run get_content_types to investigate.)");
        }
        else
        {
            foreach (var (typeName, displayName, description, properties) in types)
            {
                sb.AppendLine($"  - {typeName}" + (displayName != typeName ? $" ({displayName})" : string.Empty));
                if (!string.IsNullOrWhiteSpace(description))
                    sb.AppendLine($"    Description: {description}");
                if (properties.Count > 0)
                    sb.AppendLine($"    Properties: {string.Join(", ", properties)}");
            }
        }

        sb.AppendLine();

        if (pageType is not null)
        {
            sb.AppendLine($"The user has requested page type: {pageType}");
            sb.AppendLine();
        }

        sb.AppendLine("Steps to complete this task:");
        sb.AppendLine($"1. {(pageType is null ? "Choose the most appropriate page type from the list above, or ask the user if it is unclear." : $"Use the requested page type: {pageType}")}");
        sb.AppendLine($"2. Ask the user which properties they want to set for the chosen page type.");
        sb.AppendLine($"3. Call create_content with:");
        sb.AppendLine($"     contentType: \"<chosen type name>\"");
        sb.AppendLine($"     parentId: \"{parentId}\"");
        sb.AppendLine($"     name: \"{name}\"");
        sb.AppendLine($"     propertiesJson: <JSON object with property name/value pairs>");
        if (language is not null)
            sb.AppendLine($"     (Note: after creation, the content will be in the master language. Use update_content with language: \"{language}\" to set language-specific properties.)");
        sb.AppendLine($"4. Call publish_content with the content ID returned from create_content to make the page live.");

        return sb.ToString();
    }

    [McpServerPrompt, Description("Returns a prompt that guides an AI through creating a new shared block in Optimizely CMS. Lists available block types and the steps to create and publish the block.")]
    public static string CreateBlock(
        IContentTypeRepository contentTypeRepository,
        IOptions<ContentCreationOptions> options,
        [Description("Name for the new block")] string name,
        [Description("Content ID of the folder or content assets folder to create the block in")] string parentId,
        [Description("Language branch, e.g. 'en'. Omit to use the master language.")] string? language = null,
        [Description("Block type name to use. If omitted, the prompt will ask the AI to choose.")] string? blockType = null)
    {
        var baseClassName = options.Value.BlockBaseClass;
        var baseType = ResolveType(baseClassName);
        var types = GetFilteredContentTypes(contentTypeRepository, baseType);

        var sb = new StringBuilder();
        sb.AppendLine($"Create a new shared block in Optimizely CMS with the following details:");
        sb.AppendLine($"- Name: {name}");
        sb.AppendLine($"- Parent content ID (assets folder): {parentId}");
        sb.AppendLine($"- Language: {language ?? "master language"}");
        sb.AppendLine();

        sb.AppendLine($"Available block types (inheriting from {baseClassName}):");
        if (types.Count == 0)
        {
            sb.AppendLine("  (No block types found. Run get_content_types to investigate.)");
        }
        else
        {
            foreach (var (typeName, displayName, description, properties) in types)
            {
                sb.AppendLine($"  - {typeName}" + (displayName != typeName ? $" ({displayName})" : string.Empty));
                if (!string.IsNullOrWhiteSpace(description))
                    sb.AppendLine($"    Description: {description}");
                if (properties.Count > 0)
                    sb.AppendLine($"    Properties: {string.Join(", ", properties)}");
            }
        }

        sb.AppendLine();

        if (blockType is not null)
        {
            sb.AppendLine($"The user has requested block type: {blockType}");
            sb.AppendLine();
        }

        sb.AppendLine("Steps to complete this task:");
        sb.AppendLine($"1. {(blockType is null ? "Choose the most appropriate block type from the list above, or ask the user if it is unclear." : $"Use the requested block type: {blockType}")}");
        sb.AppendLine($"2. Ask the user which properties they want to set for the chosen block type.");
        sb.AppendLine($"3. Call create_content with:");
        sb.AppendLine($"     contentType: \"<chosen type name>\"");
        sb.AppendLine($"     parentId: \"{parentId}\"");
        sb.AppendLine($"     name: \"{name}\"");
        sb.AppendLine($"     propertiesJson: <JSON object with property name/value pairs>");
        sb.AppendLine($"4. Call publish_content with the content ID returned from create_content to make the block available.");
        sb.AppendLine();
        sb.AppendLine("Note: Shared blocks are stored in the content assets folder and can be referenced from multiple pages.");

        return sb.ToString();
    }

    private static Type? ResolveType(string typeName) =>
        AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(typeName))
            .FirstOrDefault(t => t is not null);

    private static List<(string Name, string DisplayName, string Description, List<string> Properties)> GetFilteredContentTypes(
        IContentTypeRepository contentTypeRepository, Type? baseType)
    {
        return contentTypeRepository.List()
            .Where(ct => ct.ModelType is not null
                && (baseType is null || baseType.IsAssignableFrom(ct.ModelType)))
            .Select(ct => (
                Name: ct.Name,
                DisplayName: ct.DisplayName ?? ct.Name,
                Description: ct.Description ?? string.Empty,
                Properties: ct.PropertyDefinitions
                    .Where(pd => !pd.Name.StartsWith("ep", StringComparison.OrdinalIgnoreCase))
                    .Select(pd => pd.Name)
                    .ToList()
            ))
            .ToList();
    }
}
