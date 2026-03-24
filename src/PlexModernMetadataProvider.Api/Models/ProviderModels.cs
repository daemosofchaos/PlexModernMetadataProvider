using System.Globalization;

namespace PlexModernMetadataProvider.Api.Models;

public sealed class ExternalIdValue
{
    public required string Provider { get; init; }
    public required string Id { get; init; }
}

public sealed class SourceImage
{
    public required string Type { get; init; }
    public required string Url { get; init; }
    public string? Alt { get; init; }
}

public sealed class PersonCredit
{
    public required string Name { get; init; }
    public string? Role { get; init; }
    public string? Thumb { get; init; }
    public int? Order { get; init; }
}

public sealed class MovieSearchCandidate
{
    public required string SourceKey { get; init; }
    public required string SourceId { get; init; }
    public string? Title { get; init; }
    public string? OriginalTitle { get; init; }
    public DateOnly? ReleaseDate { get; init; }
    public double Popularity { get; init; }
    public bool? IsAdult { get; init; }
}

public sealed class TvShowSearchCandidate
{
    public required string SourceKey { get; init; }
    public required string SourceId { get; init; }
    public string? Title { get; init; }
    public string? OriginalTitle { get; init; }
    public DateOnly? FirstAirDate { get; init; }
    public double Popularity { get; init; }
    public bool? IsAdult { get; init; }
}

public sealed class MovieMetadataModel
{
    public required string SourceKey { get; init; }
    public required string SourceId { get; init; }
    public required string Title { get; init; }
    public string? OriginalTitle { get; init; }
    public DateOnly? ReleaseDate { get; init; }
    public string? Summary { get; init; }
    public bool? IsAdult { get; init; }
    public int? RuntimeMinutes { get; init; }
    public string? Tagline { get; init; }
    public string? Studio { get; init; }
    public double? Rating { get; init; }
    public string? RatingImage { get; init; }
    public string? ContentRating { get; init; }
    public string? ThumbUrl { get; init; }
    public string? ArtUrl { get; init; }
    public IReadOnlyList<SourceImage>? Images { get; init; }
    public IReadOnlyList<string>? Genres { get; init; }
    public IReadOnlyList<string>? Countries { get; init; }
    public IReadOnlyList<string>? Studios { get; init; }
    public IReadOnlyList<ExternalIdValue>? ExternalIds { get; init; }
    public IReadOnlyList<PersonCredit>? Cast { get; init; }
    public IReadOnlyList<PersonCredit>? Directors { get; init; }
    public IReadOnlyList<PersonCredit>? Producers { get; init; }
    public IReadOnlyList<PersonCredit>? Writers { get; init; }
}

public sealed class ExtraMetadataModel
{
    public required string SourceKey { get; init; }
    public required string SourceId { get; init; }
    public required string Title { get; init; }
    public required string Subtype { get; init; }
    public string? Summary { get; init; }
    public string? ThumbUrl { get; init; }
    public string? ArtUrl { get; init; }
    public DateOnly? OriginallyAvailableAt { get; init; }
    public int? DurationMilliseconds { get; init; }
    public int? Year { get; init; }
    public int Index { get; init; }
    public bool IsPrimary { get; init; }
}

public sealed class ShowMetadataModel
{
    public required string SourceKey { get; init; }
    public required string SourceId { get; init; }
    public required string Title { get; init; }
    public string? OriginalTitle { get; init; }
    public DateOnly? FirstAirDate { get; init; }
    public string? Summary { get; init; }
    public bool? IsAdult { get; init; }
    public int? RuntimeMinutes { get; init; }
    public string? Tagline { get; init; }
    public string? Studio { get; init; }
    public double? Rating { get; init; }
    public string? RatingImage { get; init; }
    public string? ContentRating { get; init; }
    public string? ThumbUrl { get; init; }
    public string? ArtUrl { get; init; }
    public IReadOnlyList<SourceImage>? Images { get; init; }
    public IReadOnlyList<string>? Genres { get; init; }
    public IReadOnlyList<string>? Countries { get; init; }
    public IReadOnlyList<string>? Studios { get; init; }
    public IReadOnlyList<string>? Networks { get; init; }
    public IReadOnlyList<ExternalIdValue>? ExternalIds { get; init; }
    public IReadOnlyList<PersonCredit>? Cast { get; init; }
    public IReadOnlyList<PersonCredit>? Directors { get; init; }
    public IReadOnlyList<PersonCredit>? Producers { get; init; }
    public IReadOnlyList<PersonCredit>? Writers { get; init; }
    public IReadOnlyList<SeasonMetadataModel>? Seasons { get; init; }
}

public sealed class SeasonMetadataModel
{
    public required string SourceKey { get; init; }
    public required string ShowSourceId { get; init; }
    public required int SeasonNumber { get; init; }
    public required string Title { get; init; }
    public DateOnly? AirDate { get; init; }
    public string? Summary { get; init; }
    public string? ThumbUrl { get; init; }
    public string? ArtUrl { get; init; }
    public IReadOnlyList<SourceImage>? Images { get; init; }
    public IReadOnlyList<ExternalIdValue>? ExternalIds { get; init; }
    public IReadOnlyList<EpisodeMetadataModel>? Episodes { get; init; }
}

public sealed class EpisodeMetadataModel
{
    public required string SourceKey { get; init; }
    public required string ShowSourceId { get; init; }
    public required int SeasonNumber { get; init; }
    public required int EpisodeNumber { get; init; }
    public required string Title { get; init; }
    public DateOnly? AirDate { get; init; }
    public string? Summary { get; init; }
    public int? RuntimeMinutes { get; init; }
    public double? Rating { get; init; }
    public string? RatingImage { get; init; }
    public string? ThumbUrl { get; init; }
    public string? ArtUrl { get; init; }
    public IReadOnlyList<SourceImage>? Images { get; init; }
    public IReadOnlyList<ExternalIdValue>? ExternalIds { get; init; }
    public IReadOnlyList<PersonCredit>? Cast { get; init; }
    public IReadOnlyList<PersonCredit>? Directors { get; init; }
    public IReadOnlyList<PersonCredit>? Producers { get; init; }
    public IReadOnlyList<PersonCredit>? Writers { get; init; }
}
