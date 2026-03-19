# Hangsolow.OptiMcpServer

An [MCP (Model Context Protocol)](https://modelcontextprotocol.io) server library for **Optimizely CMS 12**. Install it into your existing Optimizely project to expose content operations as tools callable by AI assistants (Claude, Copilot, etc.).

## What it does

Exposes the following MCP tools over HTTP (`/mcp`):

| Tool | Description |
|------|-------------|
| `get_content` | Fetch a single content item by ID |
| `get_children` | List children of a content node (paginated) |
| `get_ancestors` | Get the breadcrumb hierarchy for a content item |
| `get_content_types` | List all registered content types and their properties |
| `create_content` | Create a new content item (draft) |
| `update_content` | Update properties on an existing content item |
| `publish_content` | Publish a content item |
| `move_to_trash` | Soft-delete a content item |
| `resolve_url` | Resolve a friendly URL to a content reference |

Two guided prompts help AI assistants scaffold the correct tool calls:

- `create_page` — lists available page types and generates a creation workflow
- `create_block` — lists available block types and generates a creation workflow

## Requirements

- Optimizely CMS 12
- .NET 8 or .NET 10
- SQL Server (managed by your Optimizely project)

## NuGet source

Optimizely packages are distributed via a private feed. Add it to your `nuget.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

## Installation

```bash
dotnet add package Hangsolow.OptiMcpServer
```

## Integration

### `Startup.cs` (traditional Optimizely bootstrap)

```csharp
using OptiMcpServer;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCms()
                .AddOptiMcpServer(_configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
            endpoints.MapMcp(); // exposes /mcp
        });
    }
}
```

`AddOptiMcpServer` returns an `IMcpServerBuilder`, so you can chain additional tool assemblies:

```csharp
services.AddCms()
        .AddOptiMcpServer(_configuration)
        .WithToolsFromAssembly(typeof(MyCustomTool).Assembly);
```

### `appsettings.json`

```json
{
  "ConnectionStrings": {
    "EPiServerDB": "Server=.;Database=MyDb;Integrated Security=True;TrustServerCertificate=True"
  },
  "OptiMcpServer": {
    "PageBaseClass": "EPiServer.Core.PageData",
    "BlockBaseClass": "EPiServer.Core.BlockData"
  }
}
```

Set `PageBaseClass` and `BlockBaseClass` to your site's abstract base types (e.g. `MySite.Models.Pages.SitePageData`) so the `create_page` and `create_block` prompts only surface content types relevant to your project.

## MCP endpoint

Once running, the MCP server is available at:

```
http://<host>/mcp
```

Configure your AI client to connect to this URL using the **Streamable HTTP** transport.

## License

MIT
