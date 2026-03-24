using PlexModernMetadataProvider.Api.Models;

namespace PlexModernMetadataProvider.Api.Services;

public sealed class PlexMapper
{
    public MetadataResponse WrapMetadata(string identifier, IEnumerable<object> items)
    {
        var materialized = items.ToList();
        return new MetadataResponse
        {
            MediaContainer = new MediaContainer
            {
                Offset = 0,
                TotalSize = materialized.Count,
                Identifier = identifier,
                Size = materialized.Count,
                Metadata = materialized
            }
        };
    }

    public MetadataResponse WrapImages(string identifier, IEnumerable<PlexImage> images)
    {
        var materialized = DistinctImages(images).ToList();
        return new MetadataResponse
        {
            MediaContainer = new MediaContainer
            {
                Offset = 0,
                TotalSize = materialized.Count,
                Identifier = identifier,
                Size = materialized.Count,
                Image = materialized
            }
        };
    }

    public MovieMetadataItem MapMovie(MovieMetadataModel movie, string? primaryExtraKey = null)
    {
        var ratingKey = RatingKeys.BuildMovie(movie.SourceKey, movie.SourceId);

        return new MovieMetadataItem
        {
            RatingKey = ratingKey,
            Key = RatingKeys.BuildMetadataKey(ProviderDefinitions.MovieBasePath, ratingKey),
            Guid = RatingKeys.BuildMovieGuid(ratingKey),
            Title = movie.Title,
            OriginallyAvailableAt = SafeDate(movie.ReleaseDate),
            Thumb = movie.ThumbUrl,
            Art = movie.ArtUrl,
            ContentRating = movie.ContentRating,
            OriginalTitle = DifferentOrNull(movie.OriginalTitle, movie.Title),
            TitleSort = TitleSort(movie.Title),
            Year = movie.ReleaseDate?.Year,
            Summary = NullIfWhiteSpace(movie.Summary),
            IsAdult = movie.IsAdult,
            Duration = RuntimeMilliseconds(movie.RuntimeMinutes),
            Tagline = NullIfWhiteSpace(movie.Tagline),
            Studio = NullIfWhiteSpace(movie.Studio),
            PrimaryExtraKey = NullIfWhiteSpace(primaryExtraKey),
            Image = MovieImages(movie),
            Genre = Tags(movie.Genres),
            ExtensionData = BuildMovieExtensionData(movie),
            Country = Tags(movie.Countries),
            Role = People(movie.Cast),
            Director = People(movie.Directors),
            Producer = People(movie.Producers),
            Writer = People(movie.Writers),
            Rating = Rating(movie.Rating, movie.RatingImage ?? $"{movie.SourceKey}://image.rating")
        };
    }

    public ClipMetadataItem MapMovieExtra(MovieMetadataModel movie, ExtraMetadataModel extra)
    {
        var ratingKey = RatingKeys.BuildMovieExtra(movie.SourceKey, movie.SourceId, extra.Index);
        return new ClipMetadataItem
        {
            RatingKey = ratingKey,
            Key = RatingKeys.BuildMetadataKey(ProviderDefinitions.MovieBasePath, ratingKey),
            Guid = RatingKeys.BuildClipGuid(ProviderDefinitions.MovieIdentifier, ratingKey),
            Title = extra.Title,
            Summary = NullIfWhiteSpace(extra.Summary),
            Thumb = extra.ThumbUrl ?? movie.ThumbUrl,
            Art = extra.ArtUrl ?? movie.ArtUrl,
            Duration = extra.DurationMilliseconds,
            OriginallyAvailableAt = SafeDate(extra.OriginallyAvailableAt ?? movie.ReleaseDate),
            Year = extra.Year ?? extra.OriginallyAvailableAt?.Year ?? movie.ReleaseDate?.Year,
            Index = extra.Index,
            Subtype = extra.Subtype
        };
    }

    public ShowMetadataItem MapShow(ShowMetadataModel show, bool includeChildren)
    {
        var ratingKey = RatingKeys.BuildShow(show.SourceKey, show.SourceId);
        var seasons = show.Seasons ?? [];

        return new ShowMetadataItem
        {
            RatingKey = ratingKey,
            Key = RatingKeys.BuildMetadataKey(ProviderDefinitions.TvBasePath, ratingKey),
            Guid = RatingKeys.BuildShowGuid(ratingKey),
            Title = show.Title,
            OriginallyAvailableAt = SafeDate(show.FirstAirDate),
            Thumb = show.ThumbUrl,
            Art = show.ArtUrl,
            ContentRating = show.ContentRating,
            OriginalTitle = DifferentOrNull(show.OriginalTitle, show.Title),
            TitleSort = TitleSort(show.Title),
            Year = show.FirstAirDate?.Year,
            Summary = NullIfWhiteSpace(show.Summary),
            IsAdult = show.IsAdult,
            Duration = RuntimeMilliseconds(show.RuntimeMinutes),
            Tagline = NullIfWhiteSpace(show.Tagline),
            Studio = NullIfWhiteSpace(show.Studio),
            Image = ShowImages(show),
            Genre = Tags(show.Genres),
            ExtensionData = BuildShowExtensionData(show),
            Country = Tags(show.Countries),
            Role = People(show.Cast),
            Director = People(show.Directors),
            Producer = People(show.Producers),
            Writer = People(show.Writers),
            Rating = Rating(show.Rating, show.RatingImage ?? $"{show.SourceKey}://image.rating"),
            Network = Tags(show.Networks),
            Children = includeChildren
                ? new ChildrenContainer
                {
                    Size = seasons.Count,
                    Metadata = seasons.Select(season => (object)MapSeason(show, season, includeChildren: false)).ToList()
                }
                : null
        };
    }

    public ClipMetadataItem MapShowExtra(ShowMetadataModel show, ExtraMetadataModel extra)
    {
        var ratingKey = RatingKeys.BuildShowExtra(show.SourceKey, show.SourceId, extra.Index);
        return new ClipMetadataItem
        {
            RatingKey = ratingKey,
            Key = RatingKeys.BuildMetadataKey(ProviderDefinitions.TvBasePath, ratingKey),
            Guid = RatingKeys.BuildClipGuid(ProviderDefinitions.TvIdentifier, ratingKey),
            Title = extra.Title,
            Summary = NullIfWhiteSpace(extra.Summary),
            Thumb = extra.ThumbUrl ?? show.ThumbUrl,
            Art = extra.ArtUrl ?? show.ArtUrl,
            Duration = extra.DurationMilliseconds,
            OriginallyAvailableAt = SafeDate(extra.OriginallyAvailableAt ?? show.FirstAirDate),
            Year = extra.Year ?? extra.OriginallyAvailableAt?.Year ?? show.FirstAirDate?.Year,
            Index = extra.Index,
            Subtype = extra.Subtype
        };
    }

    public SeasonMetadataItem MapSeason(ShowMetadataModel show, SeasonMetadataModel season, bool includeChildren)
    {
        var showRatingKey = RatingKeys.BuildShow(show.SourceKey, show.SourceId);
        var seasonRatingKey = RatingKeys.BuildSeason(show.SourceKey, show.SourceId, season.SeasonNumber);
        var episodes = season.Episodes ?? [];

        return new SeasonMetadataItem
        {
            RatingKey = seasonRatingKey,
            Key = RatingKeys.BuildMetadataKey(ProviderDefinitions.TvBasePath, seasonRatingKey),
            Guid = RatingKeys.BuildSeasonGuid(seasonRatingKey),
            Title = season.Title,
            OriginallyAvailableAt = SafeDate(season.AirDate ?? show.FirstAirDate),
            Thumb = season.ThumbUrl ?? show.ThumbUrl,
            Art = season.ArtUrl ?? show.ArtUrl,
            Year = season.AirDate?.Year,
            Summary = NullIfWhiteSpace(season.Summary),
            ParentRatingKey = showRatingKey,
            ParentKey = RatingKeys.BuildMetadataKey(ProviderDefinitions.TvBasePath, showRatingKey),
            ParentGuid = RatingKeys.BuildShowGuid(showRatingKey),
            ParentTitle = show.Title,
            ParentThumb = show.ThumbUrl,
            Index = season.SeasonNumber,
            Image = SeasonImages(show, season),
            ExtensionData = BuildGuidExtensionData(BuildGuidList(season.ExternalIds, new ExternalIdValue { Provider = season.SourceKey, Id = $"{season.ShowSourceId}:{season.SeasonNumber}" })),
            Children = includeChildren
                ? new ChildrenContainer
                {
                    Size = episodes.Count,
                    Metadata = episodes.Select(episode => (object)MapEpisode(show, season, episode)).ToList()
                }
                : null
        };
    }

    public ClipMetadataItem MapSeasonExtra(ShowMetadataModel show, SeasonMetadataModel season, ExtraMetadataModel extra)
    {
        var ratingKey = RatingKeys.BuildSeasonExtra(show.SourceKey, show.SourceId, season.SeasonNumber, extra.Index);
        return new ClipMetadataItem
        {
            RatingKey = ratingKey,
            Key = RatingKeys.BuildMetadataKey(ProviderDefinitions.TvBasePath, ratingKey),
            Guid = RatingKeys.BuildClipGuid(ProviderDefinitions.TvIdentifier, ratingKey),
            Title = extra.Title,
            Summary = NullIfWhiteSpace(extra.Summary),
            Thumb = extra.ThumbUrl ?? season.ThumbUrl ?? show.ThumbUrl,
            Art = extra.ArtUrl ?? season.ArtUrl ?? show.ArtUrl,
            Duration = extra.DurationMilliseconds,
            OriginallyAvailableAt = SafeDate(extra.OriginallyAvailableAt ?? season.AirDate ?? show.FirstAirDate),
            Year = extra.Year ?? extra.OriginallyAvailableAt?.Year ?? season.AirDate?.Year ?? show.FirstAirDate?.Year,
            Index = extra.Index,
            Subtype = extra.Subtype
        };
    }

    public EpisodeMetadataItem MapEpisode(ShowMetadataModel show, SeasonMetadataModel season, EpisodeMetadataModel episode)
    {
        var showRatingKey = RatingKeys.BuildShow(show.SourceKey, show.SourceId);
        var seasonRatingKey = RatingKeys.BuildSeason(show.SourceKey, show.SourceId, season.SeasonNumber);
        var episodeRatingKey = RatingKeys.BuildEpisode(show.SourceKey, show.SourceId, season.SeasonNumber, episode.EpisodeNumber);

        return new EpisodeMetadataItem
        {
            RatingKey = episodeRatingKey,
            Key = RatingKeys.BuildMetadataKey(ProviderDefinitions.TvBasePath, episodeRatingKey),
            Guid = RatingKeys.BuildEpisodeGuid(episodeRatingKey),
            Title = episode.Title,
            OriginallyAvailableAt = SafeDate(episode.AirDate ?? season.AirDate ?? show.FirstAirDate),
            Thumb = episode.ThumbUrl ?? show.ThumbUrl,
            Art = episode.ArtUrl ?? show.ArtUrl,
            Year = episode.AirDate?.Year,
            Summary = NullIfWhiteSpace(episode.Summary),
            Duration = RuntimeMilliseconds(episode.RuntimeMinutes ?? show.RuntimeMinutes),
            ParentRatingKey = seasonRatingKey,
            ParentKey = RatingKeys.BuildMetadataKey(ProviderDefinitions.TvBasePath, seasonRatingKey),
            ParentGuid = RatingKeys.BuildSeasonGuid(seasonRatingKey),
            ParentTitle = season.Title,
            ParentThumb = season.ThumbUrl ?? show.ThumbUrl,
            Index = episode.EpisodeNumber,
            GrandparentRatingKey = showRatingKey,
            GrandparentKey = RatingKeys.BuildMetadataKey(ProviderDefinitions.TvBasePath, showRatingKey),
            GrandparentGuid = RatingKeys.BuildShowGuid(showRatingKey),
            GrandparentTitle = show.Title,
            GrandparentThumb = show.ThumbUrl,
            ParentIndex = season.SeasonNumber,
            Image = EpisodeImages(show, episode),
            ExtensionData = BuildGuidExtensionData(BuildGuidList(episode.ExternalIds, new ExternalIdValue { Provider = episode.SourceKey, Id = $"{episode.ShowSourceId}:{episode.SeasonNumber}:{episode.EpisodeNumber}" })),
            Rating = Rating(episode.Rating, episode.RatingImage ?? $"{episode.SourceKey}://image.rating"),
            Role = People(episode.Cast),
            Director = People(episode.Directors),
            Producer = People(episode.Producers),
            Writer = People(episode.Writers)
        };
    }

    public ClipMetadataItem MapEpisodeExtra(ShowMetadataModel show, SeasonMetadataModel season, EpisodeMetadataModel episode, ExtraMetadataModel extra)
    {
        var ratingKey = RatingKeys.BuildEpisodeExtra(show.SourceKey, show.SourceId, season.SeasonNumber, episode.EpisodeNumber, extra.Index);
        return new ClipMetadataItem
        {
            RatingKey = ratingKey,
            Key = RatingKeys.BuildMetadataKey(ProviderDefinitions.TvBasePath, ratingKey),
            Guid = RatingKeys.BuildClipGuid(ProviderDefinitions.TvIdentifier, ratingKey),
            Title = extra.Title,
            Summary = NullIfWhiteSpace(extra.Summary),
            Thumb = extra.ThumbUrl ?? episode.ThumbUrl ?? show.ThumbUrl,
            Art = extra.ArtUrl ?? episode.ArtUrl ?? show.ArtUrl,
            Duration = extra.DurationMilliseconds,
            OriginallyAvailableAt = SafeDate(extra.OriginallyAvailableAt ?? episode.AirDate ?? season.AirDate ?? show.FirstAirDate),
            Year = extra.Year ?? extra.OriginallyAvailableAt?.Year ?? episode.AirDate?.Year ?? season.AirDate?.Year ?? show.FirstAirDate?.Year,
            Index = extra.Index,
            Subtype = extra.Subtype
        };
    }

    public List<PlexImage>? MovieImages(MovieMetadataModel movie)
        => ToPlexImages(movie.Images, Fallback(movie.ThumbUrl, "coverPoster", movie.Title), Fallback(movie.ArtUrl, "background", movie.Title));

    public List<PlexImage>? MovieExtraImages(ExtraMetadataModel extra)
        => ToPlexImages(
            null,
            Fallback(extra.ThumbUrl, "snapshot", extra.Title),
            Fallback(extra.ArtUrl, "background", extra.Title));

    public List<PlexImage>? ShowImages(ShowMetadataModel show)
        => ToPlexImages(show.Images, Fallback(show.ThumbUrl, "coverPoster", show.Title), Fallback(show.ArtUrl, "background", show.Title));

    public List<PlexImage>? ShowExtraImages(ExtraMetadataModel extra)
        => ToPlexImages(
            null,
            Fallback(extra.ThumbUrl, "snapshot", extra.Title),
            Fallback(extra.ArtUrl, "background", extra.Title));

    public List<PlexImage>? SeasonImages(ShowMetadataModel show, SeasonMetadataModel season)
        => ToPlexImages(season.Images, Fallback(season.ThumbUrl ?? show.ThumbUrl, "coverPoster", season.Title), Fallback(season.ArtUrl ?? show.ArtUrl, "background", show.Title));

    public List<PlexImage>? SeasonExtraImages(ShowMetadataModel show, SeasonMetadataModel season, ExtraMetadataModel extra)
        => ToPlexImages(
            null,
            Fallback(extra.ThumbUrl ?? season.ThumbUrl ?? show.ThumbUrl, "snapshot", extra.Title),
            Fallback(extra.ArtUrl ?? season.ArtUrl ?? show.ArtUrl, "background", season.Title));

    public List<PlexImage>? EpisodeImages(ShowMetadataModel show, EpisodeMetadataModel episode)
        => ToPlexImages(episode.Images, Fallback(episode.ThumbUrl ?? show.ThumbUrl, "snapshot", episode.Title), Fallback(episode.ArtUrl ?? show.ArtUrl, "background", show.Title));

    public List<PlexImage>? EpisodeExtraImages(ShowMetadataModel show, SeasonMetadataModel season, EpisodeMetadataModel episode, ExtraMetadataModel extra)
        => ToPlexImages(
            null,
            Fallback(extra.ThumbUrl ?? episode.ThumbUrl ?? show.ThumbUrl, "snapshot", extra.Title),
            Fallback(extra.ArtUrl ?? episode.ArtUrl ?? season.ArtUrl ?? show.ArtUrl, "background", episode.Title));

    private static string SafeDate(DateOnly? value)
        => value?.ToString("yyyy-MM-dd") ?? "1900-01-01";

    private static int? RuntimeMilliseconds(int? runtimeMinutes)
        => runtimeMinutes is > 0 ? runtimeMinutes.Value * 60_000 : null;

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? DifferentOrNull(string? value, string? current)
        => string.Equals(value, current, StringComparison.OrdinalIgnoreCase) ? null : NullIfWhiteSpace(value);

    private static string? TitleSort(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        return title.StartsWith("The ", StringComparison.OrdinalIgnoreCase) ? $"{title[4..]}, The"
            : title.StartsWith("A ", StringComparison.OrdinalIgnoreCase) ? $"{title[2..]}, A"
            : title.StartsWith("An ", StringComparison.OrdinalIgnoreCase) ? $"{title[3..]}, An"
            : null;
    }

    private static List<PlexTag>? Tags(IEnumerable<string>? items)
    {
        var tags = items?
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => new PlexTag { Tag = item.Trim() })
            .DistinctBy(item => item.Tag, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return tags is { Count: > 0 } ? tags : null;
    }

    private static List<PlexGuid> BuildGuidList(IReadOnlyList<ExternalIdValue>? externalIds, params ExternalIdValue[] fallbacks)
    {
        var values = (externalIds ?? [])
            .Concat(fallbacks)
            .Where(item => !string.IsNullOrWhiteSpace(item.Provider) && !string.IsNullOrWhiteSpace(item.Id))
            .Select(item => $"{item.Provider.ToLowerInvariant()}://{item.Id}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(item => new PlexGuid { Id = item })
            .ToList();

        return values.Count == 0 ? [new PlexGuid { Id = "unknown://unknown" }] : values;
    }

    private static Dictionary<string, object?> BuildGuidExtensionData(List<PlexGuid> guidList)
        => new()
        {
            ["Guid"] = guidList
        };

    private static Dictionary<string, object?> BuildMovieExtensionData(MovieMetadataModel movie)
    {
        var data = BuildGuidExtensionData(BuildGuidList(movie.ExternalIds, new ExternalIdValue { Provider = movie.SourceKey, Id = movie.SourceId }));
        var studios = Tags(movie.Studios);
        if (studios is { Count: > 0 })
        {
            data["Studio"] = studios;
        }

        return data;
    }

    private static Dictionary<string, object?> BuildShowExtensionData(ShowMetadataModel show)
    {
        var data = BuildGuidExtensionData(BuildGuidList(show.ExternalIds, new ExternalIdValue { Provider = show.SourceKey, Id = show.SourceId }));
        var studios = Tags(show.Studios);
        if (studios is { Count: > 0 })
        {
            data["Studio"] = studios;
        }

        return data;
    }

    private static List<PlexPerson>? People(IReadOnlyList<PersonCredit>? items)
    {
        var people = items?
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => new PlexPerson
            {
                Tag = item.Name,
                Role = NullIfWhiteSpace(item.Role),
                Thumb = NullIfWhiteSpace(item.Thumb),
                Order = item.Order
            })
            .ToList();

        return people is { Count: > 0 } ? people : null;
    }

    private static List<PlexRating>? Rating(double? value, string image)
    {
        if (!value.HasValue || value <= 0)
        {
            return null;
        }

        return [new PlexRating { Image = image, Type = "critic", Value = Math.Round(value.Value, 1) }];
    }

    private static List<PlexImage>? ToPlexImages(IReadOnlyList<SourceImage>? images, params SourceImage?[] fallbacks)
    {
        var combined = (images ?? [])
            .Concat(fallbacks.Where(item => item is not null).Cast<SourceImage>())
            .Where(item => !string.IsNullOrWhiteSpace(item.Url))
            .Select(item => new PlexImage { Type = item.Type, Url = item.Url, Alt = item.Alt })
            .ToList();

        var distinct = DistinctImages(combined).ToList();
        return distinct.Count == 0 ? null : distinct;
    }

    private static SourceImage? Fallback(string? url, string type, string? alt)
        => string.IsNullOrWhiteSpace(url) ? null : new SourceImage { Type = type, Url = url, Alt = alt };

    private static IEnumerable<PlexImage> DistinctImages(IEnumerable<PlexImage> images)
        => images.GroupBy(image => image.Url, StringComparer.OrdinalIgnoreCase).Select(group => group.First());
}
