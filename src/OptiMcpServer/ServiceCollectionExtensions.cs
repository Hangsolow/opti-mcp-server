using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using OptiMcpServer.Options;
using OptiMcpServer.Prompts;
using OptiMcpServer.Tools;

[assembly: InternalsVisibleTo("OptiMcpServer.Tests")]

namespace OptiMcpServer;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Optimizely MCP server, including all built-in content tools and prompts.
    /// Call <c>endpoints.MapMcp()</c> in your middleware pipeline to expose the /mcp endpoint.
    /// </summary>
    /// <param name="services">The service collection from your Startup / Program.</param>
    /// <param name="configuration">The application configuration, used to bind <see cref="ContentCreationOptions"/>.</param>
    /// <returns>
    /// The <see cref="IMcpServerBuilder"/> so you can chain additional
    /// <c>.WithToolsFromAssembly()</c> or <c>.WithPromptsFromAssembly()</c> calls.
    /// </returns>
    public static IMcpServerBuilder AddOptiMcpServer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ContentCreationOptions>(
            configuration.GetSection(ContentCreationOptions.SectionName));

        return services
            .AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly(typeof(ContentQueryTools).Assembly)
            .WithPromptsFromAssembly(typeof(ContentCreationPrompts).Assembly);
    }
}
