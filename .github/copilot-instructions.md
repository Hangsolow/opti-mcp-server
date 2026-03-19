# Optimizely MCP Server — Copilot Instructions

## What this project does
A NuGet library (`Hangsolow.OptiMcpServer`) that adds MCP (Model Context Protocol) server tooling to an existing Optimizely CMS 12 project. It exposes content operations as tools callable by AI assistants. It runs in-process with the Optimizely SDK — it requires an Optimizely application to host it, and does not bootstrap CMS itself.

**Package ID:** `Hangsolow.OptiMcpServer`  
**Target frameworks:** `net8.0`, `net10.0`

## Architecture

```
src/OptiMcpServer/
├── ServiceCollectionExtensions.cs  # AddOptiMcpServer() extension method — consumer entry point
├── Options/
│   └── ContentCreationOptions.cs   # Bound from "OptiMcpServer" section in consumer's appsettings.json
├── Prompts/
│   └── ContentCreationPrompts.cs   # Prompts: create_page, create_block
├── Tools/
│   ├── ContentQueryTools.cs  # Read: get_content, get_children, get_ancestors
│   ├── ContentWriteTools.cs  # Write: create_content, update_content, publish_content, move_to_trash
│   ├── ContentTypeTools.cs   # Metadata: get_content_types
│   └── ContentSearchTools.cs # Search: resolve_url
└── Models/
    └── ContentDto.cs         # DTOs for tool responses (ContentDto, ContentTypeDto)
```

## Build and pack

```bash
# Restore (requires api.nuget.optimizely.com — see nuget.config at repo root)
dotnet restore

# Build (both net8.0 and net10.0)
dotnet build

# Pack NuGet package
dotnet pack src/OptiMcpServer -o ./artifacts
```

MCP endpoint (in consumer app): `http://<host>/mcp` (Streamable HTTP transport)

## Key conventions

### Adding a new MCP prompt
1. Create or open a file in `src/OptiMcpServer/Prompts/`
2. Decorate the class with `[McpServerPromptType]`
3. Add a `static` method with `[McpServerPrompt]` and `[Description("...")]`
4. Inject services as method parameters — same DI pattern as tools
5. Return `string` — it becomes a user-role `PromptMessage` sent to the AI

Prompts are discovered automatically via `WithPromptsFromAssembly(typeof(ContentCreationPrompts).Assembly)` in `ServiceCollectionExtensions.cs`.

### Adding a new MCP tool
1. Create or open a file in `src/OptiMcpServer/Tools/`
2. Decorate the class with `[McpServerToolType]`
3. Add a `static` method with `[McpServerTool]` and `[Description("...")]`
4. Inject Optimizely services as method parameters — DI auto-wires them from the `[McpServerTool]` method signature
5. Always return `string` — serialize with `System.Text.Json`

```csharp
[McpServerToolType]
public static class MyTools
{
    [McpServerTool, Description("Does something useful.")]
    public static string MyTool(
        IContentLoader contentLoader,   // injected by DI
        [Description("The content ID")] string id)
    {
        // ...
        return JsonSerializer.Serialize(result);
    }
}
```

Tool discovery is automatic via `WithToolsFromAssembly(typeof(ContentQueryTools).Assembly)` in `ServiceCollectionExtensions.cs` — no registration needed.

### Optimizely content access patterns
- **Read**: `IContentLoader.Get<IContent>(contentRef)` or `GetChildren<IContent>(parentRef)`
- **Write**: Load → cast to `(EPiServer.Data.Entity.IReadOnly)` → `CreateWritableClone()` → cast back to `IContent` → mutate → `IContentRepository.Save()`
- **Publish**: `IContentRepository.Save(writable, SaveAction.Publish | SaveAction.ForceCurrentVersion, AccessLevel.NoAccess)`
- **ContentReference parsing**: use `ContentQueryTools.ParseContentReference(id)` — handles both `"5"` (int) and `"5_3"` (ID_WorkID) formats
- **Property access**: `content.Property["PropertyName"]?.Value` — works generically across all content types
- **Property write**: `prop.ParseToSelf(stringValue)` — converts a string to the property's native type
- Skip internal system properties prefixed with `ep` when reading (see `MapToDto` in `ContentQueryTools`)

### Configuration — `OptiMcpServer` section
Bound to `ContentCreationOptions` in `Options/ContentCreationOptions.cs`. Configure in `appsettings.json`:

```json
"OptiMcpServer": {
  "PageBaseClass": "EPiServer.Core.PageData",
  "BlockBaseClass": "EPiServer.Core.BlockData"
}
```

Set these to your site's abstract base class (e.g. `MySite.Models.Pages.SitePageData`) to restrict the prompts to only list concrete content types relevant to the project.

The `create_page` and `create_block` prompts use `AppDomain.CurrentDomain.GetAssemblies()` to resolve the configured type name at runtime, then filter `IContentTypeRepository.List()` by `baseType.IsAssignableFrom(contentType.ModelType)`.

### Startup pattern (consumer project)
The consuming Optimizely project uses the traditional `IHostBuilder` pattern because Optimizely requires `ConfigureCmsDefaults()` on `IHostBuilder`. The library integrates via the `AddOptiMcpServer()` extension method:

```csharp
// Consumer's Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddCms()
            .AddOptiMcpServer(_configuration); // from Hangsolow.OptiMcpServer
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
```

`AddOptiMcpServer` returns `IMcpServerBuilder` so the consumer can chain `.WithToolsFromAssembly()` for their own custom tools.

### NuGet sources
Optimizely packages (`EPiServer.*`) come from `https://api.nuget.optimizely.com/v3/index.json`. The `nuget.config` at the repo root configures this alongside nuget.org. Key direct dependencies:
- `EPiServer.CMS 12.*` — umbrella package; resolves `EPiServer.CMS.Core`, `EPiServer.CMS.AspNetCore`, and UI packages
- `ModelContextProtocol.AspNetCore 1.1.0` — MCP server with Streamable HTTP transport

Note: `EPiServer.Hosting` (which provides `ConfigureCmsDefaults()`) is **not** a dependency of this library — it belongs in the consumer project's bootstrap code.

### Database
The consumer project requires SQL Server. The Optimizely initialization engine runs schema migrations automatically on first start against an empty database. Configure via the consumer's `appsettings.json` or the `ConnectionStrings__EPiServerDB` environment variable.
