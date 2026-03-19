using EPiServer.ServiceLocation;
using ModelContextProtocol.Server;
using OptiMcpServer.Options;

Host.CreateDefaultBuilder(args)
    .ConfigureCmsDefaults()
    .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
    .Build()
    .Run();

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<ContentCreationOptions>(_configuration.GetSection(ContentCreationOptions.SectionName));

        services
            .AddCms()
            .AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly()
            .WithPromptsFromAssembly();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapMcp());
    }
}
