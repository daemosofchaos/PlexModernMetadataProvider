using System.Text.RegularExpressions;

namespace PlexModernMetadataProvider.Api.Services;

public abstract record ParsedRatingKey(string SourceKey);
public sealed record MovieRatingKey(string SourceKey, string SourceId) : ParsedRatingKey(SourceKey);
public sealed record ShowRatingKey(string SourceKey, string SourceId) : ParsedRatingKey(SourceKey);
public sealed record SeasonRatingKey(string SourceKey, string SourceId, int SeasonNumber) : ParsedRatingKey(SourceKey);
public sealed record EpisodeRatingKey(string SourceKey, string SourceId, int SeasonNumber, int EpisodeNumber) : ParsedRatingKey(SourceKey);
public sealed record ExternalGuid(string Provider, string Id);

public static partial class RatingKeys
{
    public static string BuildMovie(string sourceKey, string sourceId) => $"movie-{NormalizeSource(sourceKey)}-{sourceId}";
    public static string BuildShow(string sourceKey, string sourceId) => $"show-{NormalizeSource(sourceKey)}-{sourceId}";
    public static string BuildSeason(string sourceKey, string sourceId, int seasonNumber) => $"season-{NormalizeSource(sourceKey)}-{sourceId}-{seasonNumber}";
    public static string BuildEpisode(string sourceKey, string sourceId, int seasonNumber, int episodeNumber) => $"episode-{NormalizeSource(sourceKey)}-{sourceId}-{seasonNumber}-{episodeNumber}";

    public static string BuildMetadataKey(string basePath, string ratingKey) => $"{basePath}/library/metadata/{ratingKey}";
    public static string BuildMovieGuid(string ratingKey) => $"{ProviderDefinitions.MovieIdentifier}://movie/{ratingKey}";
    public static string BuildShowGuid(string ratingKey) => $"{ProviderDefinitions.TvIdentifier}://show/{ratingKey}";
    public static string BuildSeasonGuid(string ratingKey) => $"{ProviderDefinitions.TvIdentifier}://season/{ratingKey}";
    public static string BuildEpisodeGuid(string ratingKey) => $"{ProviderDefinitions.TvIdentifier}://episode/{ratingKey}";

    public static ParsedRatingKey Parse(string ratingKey)
    {
        var movie = MovieRegex().Match(ratingKey);
        if (movie.Success)
        {
            return new MovieRatingKey(movie.Groups[1].Value.ToLowerInvariant(), movie.Groups[2].Value);
        }

        var show = ShowRegex().Match(ratingKey);
        if (show.Success)
        {
            return new ShowRatingKey(show.Groups[1].Value.ToLowerInvariant(), show.Groups[2].Value);
        }

        var season = SeasonRegex().Match(ratingKey);
        if (season.Success)
        {
            return new SeasonRatingKey(season.Groups[1].Value.ToLowerInvariant(), season.Groups[2].Value, int.Parse(season.Groups[3].Value));
        }

        var episode = EpisodeRegex().Match(ratingKey);
        if (episode.Success)
        {
            return new EpisodeRatingKey(
                episode.Groups[1].Value.ToLowerInvariant(),
                episode.Groups[2].Value,
                int.Parse(episode.Groups[3].Value),
                int.Parse(episode.Groups[4].Value));
        }

        throw new InvalidOperationException($"Unsupported rating key '{ratingKey}'.");
    }

    public static ExternalGuid? ParseExternalGuid(string? guid)
    {
        if (string.IsNullOrWhiteSpace(guid))
        {
            return null;
        }

        var match = ExternalGuidRegex().Match(guid.Trim());
        if (!match.Success)
        {
            return null;
        }

        return new ExternalGuid(match.Groups[1].Value.ToLowerInvariant(), match.Groups[2].Value);
    }

    private static string NormalizeSource(string sourceKey)
        => sourceKey.Trim().ToLowerInvariant();

    [GeneratedRegex("^movie-([a-z0-9]+)-([A-Za-z0-9]+)$", RegexOptions.Compiled)]
    private static partial Regex MovieRegex();

    [GeneratedRegex("^show-([a-z0-9]+)-([A-Za-z0-9]+)$", RegexOptions.Compiled)]
    private static partial Regex ShowRegex();

    [GeneratedRegex("^season-([a-z0-9]+)-([A-Za-z0-9]+)-(\\d+)$", RegexOptions.Compiled)]
    private static partial Regex SeasonRegex();

    [GeneratedRegex("^episode-([a-z0-9]+)-([A-Za-z0-9]+)-(\\d+)-(\\d+)$", RegexOptions.Compiled)]
    private static partial Regex EpisodeRegex();

    [GeneratedRegex("^([^:]+)://(.+)$", RegexOptions.Compiled)]
    private static partial Regex ExternalGuidRegex();
}
