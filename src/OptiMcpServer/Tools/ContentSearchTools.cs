using System.ComponentModel;
using System.Text.Json;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Web.Routing;
using ModelContextProtocol.Server;
using OptiMcpServer.Models;

namespace OptiMcpServer.Tools;

[McpServerToolType]
public static class ContentSearchTools
{
    [McpServerTool, Description("Resolve a relative URL path to a content item. Returns the content and its ID, which can be used with other tools. Example paths: '/en/about-us/', '/products/widget'.")]
    public static string ResolveUrl(
        IUrlResolver urlResolver,
        IContentTypeRepository contentTypeRepository,
        [Description("The relative URL path to resolve, e.g. '/en/about-us/'")] string url)
    {
        try
        {
            var urlBuilder = new UrlBuilder(url);
            var content = urlResolver.Route(urlBuilder);

            if (content is null)
            {
                return JsonSerializer.Serialize(new { error = $"No content found for URL '{url}'." });
            }

            var dto = ContentQueryTools.MapToDto(content, contentTypeRepository);
            return JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
