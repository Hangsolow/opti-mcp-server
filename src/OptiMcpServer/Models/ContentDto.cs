using System.Text.Json.Serialization;

namespace OptiMcpServer.Models;

public class ContentDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("contentTypeId")]
    public int ContentTypeId { get; set; }

    [JsonPropertyName("contentTypeName")]
    public string ContentTypeName { get; set; } = string.Empty;

    [JsonPropertyName("parentId")]
    public string ParentId { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public DateTime? Created { get; set; }

    [JsonPropertyName("changed")]
    public DateTime? Changed { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, string?> Properties { get; set; } = [];
}

public class ContentTypeDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("properties")]
    public List<ContentTypePropertyDto> Properties { get; set; } = [];
}

public class ContentTypePropertyDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; }
}
