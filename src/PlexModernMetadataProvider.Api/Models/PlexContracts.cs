using System.Text.Json.Serialization;

namespace PlexModernMetadataProvider.Api.Models;

public sealed class MediaProviderResponse
{
    [JsonPropertyName("MediaProvider")]
    public required MediaProviderDefinition MediaProvider { get; init; }
}

public sealed class MediaProviderDefinition
{
    [JsonPropertyName("identifier")]
    public required string Identifier { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; init; }

    [JsonPropertyName("Types")]
    public required List<ProviderTypeDefinition> Types { get; init; }

    [JsonPropertyName("Feature")]
    public required List<ProviderFeature> Feature { get; init; }
}

public sealed class ProviderTypeDefinition
{
    [JsonPropertyName("type")]
    public required int Type { get; init; }

    [JsonPropertyName("Scheme")]
    public required List<ProviderScheme> Scheme { get; init; }
}

public sealed class ProviderScheme
{
    [JsonPropertyName("scheme")]
    public required string Scheme { get; init; }
}

public sealed class ProviderFeature
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("key")]
    public required string Key { get; init; }
}

public sealed class MetadataResponse
{
    [JsonPropertyName("MediaContainer")]
    public required MediaContainer MediaContainer { get; init; }
}

public sealed class MediaContainer
{
    [JsonPropertyName("offset")]
    public required int Offset { get; init; }

    [JsonPropertyName("totalSize")]
    public required int TotalSize { get; init; }

    [JsonPropertyName("identifier")]
    public required string Identifier { get; init; }

    [JsonPropertyName("size")]
    public required int Size { get; init; }

    [JsonPropertyName("Metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<object>? Metadata { get; init; }

    [JsonPropertyName("Image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexImage>? Image { get; init; }
}

public sealed class ChildrenContainer
{
    [JsonPropertyName("size")]
    public required int Size { get; init; }

    [JsonPropertyName("Metadata")]
    public required List<object> Metadata { get; init; }
}

public sealed class PlexImage
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("alt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Alt { get; init; }
}

public sealed class PlexGuid
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
}

public sealed class PlexTag
{
    [JsonPropertyName("tag")]
    public required string Tag { get; init; }
}

public sealed class PlexPerson
{
    [JsonPropertyName("tag")]
    public required string Tag { get; init; }

    [JsonPropertyName("thumb")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Thumb { get; init; }

    [JsonPropertyName("role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Role { get; init; }

    [JsonPropertyName("order")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Order { get; init; }
}

public sealed class PlexRating
{
    [JsonPropertyName("image")]
    public required string Image { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("value")]
    public required double Value { get; init; }
}

public sealed class MovieMetadataItem
{
    [JsonPropertyName("ratingKey")]
    public required string RatingKey { get; init; }

    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("guid")]
    public required string Guid { get; init; }

    [JsonPropertyName("type")]
    public string Type => "movie";

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("originallyAvailableAt")]
    public required string OriginallyAvailableAt { get; init; }

    [JsonPropertyName("thumb")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Thumb { get; init; }

    [JsonPropertyName("art")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Art { get; init; }

    [JsonPropertyName("contentRating")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContentRating { get; init; }

    [JsonPropertyName("originalTitle")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OriginalTitle { get; init; }

    [JsonPropertyName("titleSort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TitleSort { get; init; }

    [JsonPropertyName("year")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Year { get; init; }

    [JsonPropertyName("summary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Summary { get; init; }

    [JsonPropertyName("isAdult")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsAdult { get; init; }

    [JsonPropertyName("duration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Duration { get; init; }

    [JsonPropertyName("tagline")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tagline { get; init; }

    [JsonPropertyName("studio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Studio { get; init; }

    [JsonPropertyName("Image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexImage>? Image { get; init; }

    [JsonPropertyName("Genre")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexTag>? Genre { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object?>? ExtensionData { get; init; }

    [JsonPropertyName("Country")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexTag>? Country { get; init; }

    [JsonPropertyName("Role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexPerson>? Role { get; init; }

    [JsonPropertyName("Director")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexPerson>? Director { get; init; }

    [JsonPropertyName("Producer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexPerson>? Producer { get; init; }

    [JsonPropertyName("Writer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexPerson>? Writer { get; init; }

    [JsonPropertyName("Rating")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexRating>? Rating { get; init; }
}

public sealed class ShowMetadataItem
{
    [JsonPropertyName("ratingKey")]
    public required string RatingKey { get; init; }

    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("guid")]
    public required string Guid { get; init; }

    [JsonPropertyName("type")]
    public string Type => "show";

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("originallyAvailableAt")]
    public required string OriginallyAvailableAt { get; init; }

    [JsonPropertyName("thumb")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Thumb { get; init; }

    [JsonPropertyName("art")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Art { get; init; }

    [JsonPropertyName("contentRating")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContentRating { get; init; }

    [JsonPropertyName("originalTitle")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OriginalTitle { get; init; }

    [JsonPropertyName("titleSort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TitleSort { get; init; }

    [JsonPropertyName("year")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Year { get; init; }

    [JsonPropertyName("summary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Summary { get; init; }

    [JsonPropertyName("isAdult")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsAdult { get; init; }

    [JsonPropertyName("duration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Duration { get; init; }

    [JsonPropertyName("tagline")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tagline { get; init; }

    [JsonPropertyName("studio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Studio { get; init; }

    [JsonPropertyName("Image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexImage>? Image { get; init; }

    [JsonPropertyName("Genre")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexTag>? Genre { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object?>? ExtensionData { get; init; }

    [JsonPropertyName("Country")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexTag>? Country { get; init; }

    [JsonPropertyName("Role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexPerson>? Role { get; init; }

    [JsonPropertyName("Director")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexPerson>? Director { get; init; }

    [JsonPropertyName("Producer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexPerson>? Producer { get; init; }

    [JsonPropertyName("Writer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexPerson>? Writer { get; init; }

    [JsonPropertyName("Rating")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexRating>? Rating { get; init; }

    [JsonPropertyName("Network")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexTag>? Network { get; init; }

    [JsonPropertyName("Children")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ChildrenContainer? Children { get; init; }
}

public sealed class SeasonMetadataItem
{
    [JsonPropertyName("ratingKey")]
    public required string RatingKey { get; init; }

    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("guid")]
    public required string Guid { get; init; }

    [JsonPropertyName("type")]
    public string Type => "season";

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("originallyAvailableAt")]
    public required string OriginallyAvailableAt { get; init; }

    [JsonPropertyName("thumb")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Thumb { get; init; }

    [JsonPropertyName("art")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Art { get; init; }

    [JsonPropertyName("year")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Year { get; init; }

    [JsonPropertyName("summary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Summary { get; init; }

    [JsonPropertyName("parentRatingKey")]
    public required string ParentRatingKey { get; init; }

    [JsonPropertyName("parentKey")]
    public required string ParentKey { get; init; }

    [JsonPropertyName("parentGuid")]
    public required string ParentGuid { get; init; }

    [JsonPropertyName("parentType")]
    public string ParentType => "show";

    [JsonPropertyName("parentTitle")]
    public required string ParentTitle { get; init; }

    [JsonPropertyName("parentThumb")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ParentThumb { get; init; }

    [JsonPropertyName("index")]
    public required int Index { get; init; }

    [JsonPropertyName("Image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexImage>? Image { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object?>? ExtensionData { get; init; }

    [JsonPropertyName("Children")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ChildrenContainer? Children { get; init; }
}

public sealed class EpisodeMetadataItem
{
    [JsonPropertyName("ratingKey")]
    public required string RatingKey { get; init; }

    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("guid")]
    public required string Guid { get; init; }

    [JsonPropertyName("type")]
    public string Type => "episode";

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("originallyAvailableAt")]
    public required string OriginallyAvailableAt { get; init; }

    [JsonPropertyName("thumb")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Thumb { get; init; }

    [JsonPropertyName("art")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Art { get; init; }

    [JsonPropertyName("year")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Year { get; init; }

    [JsonPropertyName("summary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Summary { get; init; }

    [JsonPropertyName("duration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Duration { get; init; }

    [JsonPropertyName("parentRatingKey")]
    public required string ParentRatingKey { get; init; }

    [JsonPropertyName("parentKey")]
    public required string ParentKey { get; init; }

    [JsonPropertyName("parentGuid")]
    public required string ParentGuid { get; init; }

    [JsonPropertyName("parentType")]
    public string ParentType => "season";

    [JsonPropertyName("parentTitle")]
    public required string ParentTitle { get; init; }

    [JsonPropertyName("parentThumb")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ParentThumb { get; init; }

    [JsonPropertyName("index")]
    public required int Index { get; init; }

    [JsonPropertyName("grandparentRatingKey")]
    public required string GrandparentRatingKey { get; init; }

    [JsonPropertyName("grandparentKey")]
    public required string GrandparentKey { get; init; }

    [JsonPropertyName("grandparentGuid")]
    public required string GrandparentGuid { get; init; }

    [JsonPropertyName("grandparentType")]
    public string GrandparentType => "show";

    [JsonPropertyName("grandparentTitle")]
    public required string GrandparentTitle { get; init; }

    [JsonPropertyName("grandparentThumb")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GrandparentThumb { get; init; }

    [JsonPropertyName("parentIndex")]
    public required int ParentIndex { get; init; }

    [JsonPropertyName("Image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexImage>? Image { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object?>? ExtensionData { get; init; }

    [JsonPropertyName("Rating")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexRating>? Rating { get; init; }

    [JsonPropertyName("Role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexPerson>? Role { get; init; }

    [JsonPropertyName("Director")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexPerson>? Director { get; init; }

    [JsonPropertyName("Producer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexPerson>? Producer { get; init; }

    [JsonPropertyName("Writer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexPerson>? Writer { get; init; }
}
