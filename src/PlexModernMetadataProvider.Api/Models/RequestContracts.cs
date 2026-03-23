using System.Text.Json.Serialization;

namespace PlexModernMetadataProvider.Api.Models;

public sealed class MatchRequest
{
    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("parentTitle")]
    public string? ParentTitle { get; set; }

    [JsonPropertyName("grandparentTitle")]
    public string? GrandparentTitle { get; set; }

    [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("guid")]
    public string? Guid { get; set; }

    [JsonPropertyName("index")]
    public int? Index { get; set; }

    [JsonPropertyName("parentIndex")]
    public int? ParentIndex { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("manual")]
    public int? Manual { get; set; }

    [JsonPropertyName("includeAdult")]
    public int? IncludeAdult { get; set; }

    [JsonPropertyName("includeChildren")]
    public int? IncludeChildren { get; set; }
}

public sealed record PlexRequestContext(string Language, string Country);

public sealed record PagingOptions(int ContainerSize, int ContainerStart);
