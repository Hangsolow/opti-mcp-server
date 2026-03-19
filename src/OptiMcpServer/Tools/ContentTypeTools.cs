using System.ComponentModel;
using System.Text.Json;
using EPiServer.DataAbstraction;
using ModelContextProtocol.Server;
using OptiMcpServer.Models;

namespace OptiMcpServer.Tools;

[McpServerToolType]
public static class ContentTypeTools
{
    [McpServerTool, Description("List all content types registered in the Optimizely CMS, with their properties and types.")]
    public static string GetContentTypes(
        IContentTypeRepository contentTypeRepository)
    {
        try
        {
            var contentTypes = contentTypeRepository.List();

            var dtos = contentTypes.Select(ct => new ContentTypeDto
            {
                Id = ct.ID,
                Name = ct.Name,
                DisplayName = ct.DisplayName ?? ct.Name,
                Description = ct.Description ?? string.Empty,
                Properties = ct.PropertyDefinitions
                    .Select(pd => new ContentTypePropertyDto
                    {
                        Name = pd.Name,
                        Type = pd.Type?.Name ?? string.Empty,
                        IsRequired = pd.Required
                    })
                    .ToList()
            }).ToList();

            return JsonSerializer.Serialize(new
            {
                count = dtos.Count,
                contentTypes = dtos
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
