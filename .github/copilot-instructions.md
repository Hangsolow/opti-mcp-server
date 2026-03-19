# Optimizely MCP Server — Copilot Instructions

## What this project does
An ASP.NET Core 10 MCP (Model Context Protocol) server that exposes Optimizely CMS 12 content operations as tools callable by AI assistants. It runs in-process with the Optimizely SDK — it IS an Optimizely application, not a proxy.

## Architecture

```
src/OptiMcpServer/
├── Program.cs              # App bootstrap: Optimizely CMS + MCP server wired together
├── Options/
│   └── ContentCreationOptions.cs  # Bound from "OptiMcpServer" section in appsettings.json
├── Prompts/
│   └── ContentCreationPrompts.cs  # Prompts: create_page, create_block
├── Tools/
│   ├── ContentQueryTools.cs  # Read: get_content, get_children, get_ancestors
│   ├── ContentWriteTools.cs  # Write: create_content, update_content, publish_content, move_to_trash
│   ├── ContentTypeTools.cs   # Metadata: get_content_types
│   └── ContentSearchTools.cs # Search: resolve_url
└── Models/
    └── ContentDto.cs         # DTOs for tool responses (ContentDto, ContentTypeDto)
```

## Build and run

```bash
# Restore (requires api.nuget.optimizely.com — see nuget.config at repo root)
dotnet restore

# Build
dotnet build

# Run (set SQL Server connection string in appsettings.json first)
dotnet run --project src/OptiMcpServer
```

MCP endpoint: `http://localhost:5000/mcp` (Streamable HTTP transport)

## Key conventions

### Adding a new MCP prompt
1. Create or open a file in `src/OptiMcpServer/Prompts/`
2. Decorate the class with `[McpServerPromptType]`
3. Add a `static` method with `[McpServerPrompt]` and `[Description("...")]`
4. Inject services as method parameters — same DI pattern as tools
5. Return `string` — it becomes a user-role `PromptMessage` sent to the AI

Prompts are discovered automatically via `WithPromptsFromAssembly()` in `Program.cs`.

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

Tool discovery is automatic via `WithToolsFromAssembly()` in `Program.cs` — no registration needed.

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

### Startup pattern
`Program.cs` uses the traditional `IHostBuilder` pattern (not minimal APIs) because Optimizely requires `ConfigureCmsDefaults()` on `IHostBuilder`:

```csharp
Host.CreateDefaultBuilder(args)
    .ConfigureCmsDefaults()   // from EPiServer.Hosting — starts the init engine
    .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
    .Build().Run();
```

MCP services and tools are registered in `Startup.ConfigureServices`, and `endpoints.MapMcp()` is called in `Startup.Configure`.

### NuGet sources
Optimizely packages (`EPiServer.*`) come from `https://api.nuget.optimizely.com/v3/index.json`. The `nuget.config` at the repo root configures this alongside nuget.org. Key direct dependencies:
- `EPiServer.CMS 12.*` — umbrella package; resolves `EPiServer.CMS.Core`, `EPiServer.CMS.AspNetCore`, and UI packages
- `EPiServer.Hosting 12.*` — provides `ConfigureCmsDefaults()` and the initialization engine
- `ModelContextProtocol.AspNetCore 1.1.0` — MCP server with Streamable HTTP transport

### Database
Requires SQL Server. The Optimizely initialization engine runs schema migrations automatically on first start against an empty database. Configure via `appsettings.json` or the `ConnectionStrings__EPiServerDB` environment variable.
