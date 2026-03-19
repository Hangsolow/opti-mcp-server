namespace OptiMcpServer.Options;

public class ContentCreationOptions
{
    public const string SectionName = "OptiMcpServer";

    /// <summary>
    /// Fully qualified type name of the base class for pages.
    /// Content types whose ModelType inherits from this class will be listed as page types.
    /// </summary>
    public string PageBaseClass { get; set; } = "EPiServer.Core.PageData";

    /// <summary>
    /// Fully qualified type name of the base class for blocks.
    /// Content types whose ModelType inherits from this class will be listed as block types.
    /// </summary>
    public string BlockBaseClass { get; set; } = "EPiServer.Core.BlockData";
}
