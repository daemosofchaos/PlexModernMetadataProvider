using PlexModernMetadataProvider.Api.Models;
using PlexModernMetadataProvider.Api.Options;

namespace PlexModernMetadataProvider.Api.Services;

public sealed class TmdbTvSource : ITvMetadataSource
{
    private const string ImageBase = "https://image.tmdb.org/t/p";
    private readonly ITmdbClient _client;
    private readonly TmdbOptions _options;

    public TmdbTvSource(ITmdbClient client, Microsoft.Extensions.Options.IOptions<ProviderOptions> options)
    {
        _client = client;
        _options = options.Value.TMDb;
    }

    public string SourceKey => "tmdb";
    public bool IsEnabled => !string.IsNullOrWhiteSpace(_options.ReadAccessToken) || !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<ShowMetadataModel?> FindByExternalGuidAsync(ExternalGuid guid, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return null;
        }

        if (guid.Provider == "tmdb" && int.TryParse(guid.Id, out var tmdbId))
        {
            return await GetShowAsync(tmdbId.ToString(), context, cancellationToken);
        }

        if (guid.Provider is not ("imdb" or "tvdb"))
        {
            return null;
        }

        var externalSource = guid.Provider == "imdb" ? "imdb_id" : "tvdb_id";
        var find = await _client.FindByExternalIdAsync(guid.Id, externalSource, context.Language, cancellationToken);
        var show = find.TvResults.FirstOrDefault();
        return show is null ? null : await GetShowAsync(show.Id.ToString(), context, cancellationToken);
    }

    public async Task<IReadOnlyList<TvShowSearchCandidate>> SearchAsync(string queryTitle, int? requestedYear, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(queryTitle))
        {
            return [];
        }

        var search = await _client.SearchTvAsync(queryTitle, context.Language, requestedYear, cancellationToken);
        return search.Results
            .Select(item => new TvShowSearchCandidate
            {
                SourceKey = SourceKey,
                SourceId = item.Id.ToString(),
                Title = item.Name,
                OriginalTitle = item.OriginalName,
                FirstAirDate = ParseDate(item.FirstAirDate),
                Popularity = item.Popularity,
                IsAdult = item.Adult
            })
            .ToList();
    }

    public async Task<ShowMetadataModel?> GetShowAsync(string sourceId, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || !int.TryParse(sourceId, out var tvId))
        {
            return null;
        }

        var show = await _client.GetTvAsync(tvId, context.Language, cancellationToken);
        return MapShow(show, includeSeasonEpisodes: false, context.Country);
    }

    public async Task<SeasonMetadataModel?> GetSeasonAsync(string sourceId, int seasonNumber, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || !int.TryParse(sourceId, out var tvId))
        {
            return null;
        }

        var show = await _client.GetTvAsync(tvId, context.Language, cancellationToken);
        var season = await _client.GetSeasonAsync(tvId, seasonNumber, context.Language, cancellationToken);
        return MapSeason(show, season, includeEpisodes: true);
    }

    public async Task<EpisodeMetadataModel?> GetEpisodeAsync(string sourceId, int seasonNumber, int episodeNumber, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || !int.TryParse(sourceId, out var tvId))
        {
            return null;
        }

        var show = await _client.GetTvAsync(tvId, context.Language, cancellationToken);
        var season = await _client.GetSeasonAsync(tvId, seasonNumber, context.Language, cancellationToken);
        var episode = await _client.GetEpisodeAsync(tvId, seasonNumber, episodeNumber, context.Language, cancellationToken);
        return MapEpisode(show, season, episode);
    }

    public async Task<EpisodeMetadataModel?> GetEpisodeByAirDateAsync(string sourceId, DateOnly airDate, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || !int.TryParse(sourceId, out var tvId))
        {
            return null;
        }

        var show = await _client.GetTvAsync(tvId, context.Language, cancellationToken);
        foreach (var seasonSummary in show.Seasons ?? [])
        {
            if (seasonSummary.SeasonNumber < 0)
            {
                continue;
            }

            var season = await _client.GetSeasonAsync(tvId, seasonSummary.SeasonNumber, context.Language, cancellationToken);
            var episode = season.Episodes?.FirstOrDefault(item => ParseDate(item.AirDate) == airDate);
            if (episode is not null)
            {
                return MapEpisode(show, season, episode);
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<ExtraMetadataModel>> GetShowExtrasAsync(string sourceId, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || !int.TryParse(sourceId, out var tvId))
        {
            return [];
        }

        var show = await _client.GetTvAsync(tvId, context.Language, cancellationToken);
        return BuildExtras(show.Videos?.Results, ImageUrl(show.PosterPath), ImageUrl(show.BackdropPath), ParseDate(show.FirstAirDate));
    }

    public async Task<IReadOnlyList<ExtraMetadataModel>> GetSeasonExtrasAsync(string sourceId, int seasonNumber, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || !int.TryParse(sourceId, out var tvId))
        {
            return [];
        }

        var show = await _client.GetTvAsync(tvId, context.Language, cancellationToken);
        var season = await _client.GetSeasonAsync(tvId, seasonNumber, context.Language, cancellationToken);
        return BuildExtras(season.Videos?.Results, ImageUrl(season.PosterPath) ?? ImageUrl(show.PosterPath), ImageUrl(show.BackdropPath), ParseDate(season.AirDate) ?? ParseDate(show.FirstAirDate));
    }

    public async Task<IReadOnlyList<ExtraMetadataModel>> GetEpisodeExtrasAsync(string sourceId, int seasonNumber, int episodeNumber, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || !int.TryParse(sourceId, out var tvId))
        {
            return [];
        }

        var show = await _client.GetTvAsync(tvId, context.Language, cancellationToken);
        var episode = await _client.GetEpisodeAsync(tvId, seasonNumber, episodeNumber, context.Language, cancellationToken);
        return BuildExtras(episode.Videos?.Results, ImageUrl(episode.StillPath) ?? ImageUrl(show.PosterPath), ImageUrl(show.BackdropPath), ParseDate(episode.AirDate) ?? ParseDate(show.FirstAirDate));
    }

    public async Task<IReadOnlyList<ExtraMetadataModel>> GetShowExtrasByExternalIdsAsync(IReadOnlyList<ExternalIdValue>? externalIds, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        var tmdbId = await ResolveTmdbShowIdAsync(externalIds, context, cancellationToken);
        return tmdbId is null ? [] : await GetShowExtrasAsync(tmdbId.Value.ToString(), context, cancellationToken);
    }

    public async Task<IReadOnlyList<ExtraMetadataModel>> GetSeasonExtrasByExternalIdsAsync(IReadOnlyList<ExternalIdValue>? externalIds, int seasonNumber, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        var tmdbId = await ResolveTmdbShowIdAsync(externalIds, context, cancellationToken);
        return tmdbId is null ? [] : await GetSeasonExtrasAsync(tmdbId.Value.ToString(), seasonNumber, context, cancellationToken);
    }

    public async Task<IReadOnlyList<ExtraMetadataModel>> GetEpisodeExtrasByExternalIdsAsync(IReadOnlyList<ExternalIdValue>? externalIds, int seasonNumber, int episodeNumber, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        var tmdbId = await ResolveTmdbShowIdAsync(externalIds, context, cancellationToken);
        return tmdbId is null ? [] : await GetEpisodeExtrasAsync(tmdbId.Value.ToString(), seasonNumber, episodeNumber, context, cancellationToken);
    }

    private async Task<int?> ResolveTmdbShowIdAsync(IReadOnlyList<ExternalIdValue>? externalIds, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var tmdbId = externalIds?
            .FirstOrDefault(item => string.Equals(item.Provider, "tmdb", StringComparison.OrdinalIgnoreCase))?
            .Id;

        if (int.TryParse(tmdbId, out var parsedTmdbId))
        {
            return parsedTmdbId;
        }

        var imdbId = externalIds?
            .FirstOrDefault(item => string.Equals(item.Provider, "imdb", StringComparison.OrdinalIgnoreCase))?
            .Id;

        if (!string.IsNullOrWhiteSpace(imdbId))
        {
            var findByImdb = await _client.FindByExternalIdAsync(imdbId, "imdb_id", context.Language, cancellationToken);
            if (findByImdb.TvResults.FirstOrDefault() is { } imdbMatch)
            {
                return imdbMatch.Id;
            }
        }

        var tvdbId = externalIds?
            .FirstOrDefault(item => string.Equals(item.Provider, "tvdb", StringComparison.OrdinalIgnoreCase))?
            .Id;

        if (!string.IsNullOrWhiteSpace(tvdbId))
        {
            var findByTvdb = await _client.FindByExternalIdAsync(tvdbId, "tvdb_id", context.Language, cancellationToken);
            if (findByTvdb.TvResults.FirstOrDefault() is { } tvdbMatch)
            {
                return tvdbMatch.Id;
            }
        }

        return null;
    }

    private static ShowMetadataModel MapShow(TmdbTvDetails show, bool includeSeasonEpisodes, string country)
    {
        var seasons = (show.Seasons ?? [])
            .Where(season => season.SeasonNumber >= 0)
            .Select(season => MapSeason(show, season, includeSeasonEpisodes))
            .ToList();

        return new ShowMetadataModel
        {
            SourceKey = "tmdb",
            SourceId = show.Id.ToString(),
            Title = show.Name ?? $"Show {show.Id}",
            OriginalTitle = DifferentOrNull(show.OriginalName, show.Name),
            FirstAirDate = ParseDate(show.FirstAirDate),
            Summary = NullIfWhiteSpace(show.Overview),
            IsAdult = show.Adult == true ? true : null,
            RuntimeMinutes = FirstPositiveRuntime(show.EpisodeRunTime),
            Tagline = NullIfWhiteSpace(show.Tagline),
            Studio = FirstName(show.Networks) ?? FirstName(show.ProductionCompanies),
            Rating = show.VoteAverage > 0 ? Math.Round(show.VoteAverage, 1) : null,
            RatingImage = "themoviedb://image.rating",
            ContentRating = TvContentRating(show, country),
            ThumbUrl = ImageUrl(show.PosterPath),
            ArtUrl = ImageUrl(show.BackdropPath),
            Images = BuildShowImages(show),
            Genres = Names(show.Genres),
            Countries = Names(show.ProductionCountries),
            Studios = Names(show.ProductionCompanies),
            Networks = Names(show.Networks),
            ExternalIds = BuildExternalIds(show.Id, show.ExternalIds?.ImdbId, show.ExternalIds?.TvdbId),
            Cast = Cast((show.AggregateCredits ?? show.Credits)?.Cast),
            Directors = Crew((show.AggregateCredits ?? show.Credits)?.Crew, "Director"),
            Producers = Crew((show.AggregateCredits ?? show.Credits)?.Crew, "Producer", "Executive Producer"),
            Writers = Crew((show.AggregateCredits ?? show.Credits)?.Crew, "Writer", "Screenplay", "Story"),
            Seasons = seasons
        };
    }

    private static SeasonMetadataModel MapSeason(TmdbTvDetails show, TmdbSeasonSummary season, bool includeEpisodes)
        => new()
        {
            SourceKey = "tmdb",
            ShowSourceId = show.Id.ToString(),
            SeasonNumber = season.SeasonNumber,
            Title = season.Name ?? (season.SeasonNumber == 0 ? "Specials" : $"Season {season.SeasonNumber}"),
            AirDate = ParseDate(season.AirDate ?? show.FirstAirDate),
            Summary = NullIfWhiteSpace(season.Overview),
            ThumbUrl = ImageUrl(season.PosterPath) ?? ImageUrl(show.PosterPath),
            ArtUrl = ImageUrl(show.BackdropPath),
            Images = BuildSeasonImages(show, season),
            ExternalIds =
            [
                new ExternalIdValue
                {
                    Provider = "tmdb",
                    Id = season.Id?.ToString() ?? $"{show.Id}:{season.SeasonNumber}"
                }
            ],
            Episodes = includeEpisodes
                ? (season.Episodes ?? [])
                    .Select(episode => MapEpisode(show, season, episode))
                    .ToList()
                : null
        };

    private static EpisodeMetadataModel MapEpisode(TmdbTvDetails show, TmdbSeasonSummary season, TmdbEpisodeSummary episode)
        => new()
        {
            SourceKey = "tmdb",
            ShowSourceId = show.Id.ToString(),
            SeasonNumber = season.SeasonNumber,
            EpisodeNumber = episode.EpisodeNumber,
            Title = episode.Name ?? $"Episode {episode.EpisodeNumber}",
            AirDate = ParseDate(episode.AirDate ?? season.AirDate ?? show.FirstAirDate),
            Summary = NullIfWhiteSpace(episode.Overview),
            RuntimeMinutes = episode.Runtime is > 0 ? episode.Runtime : FirstPositiveRuntime(show.EpisodeRunTime),
            Rating = episode.VoteAverage > 0 ? Math.Round(episode.VoteAverage, 1) : null,
            RatingImage = "themoviedb://image.rating",
            ThumbUrl = ImageUrl(episode.StillPath) ?? ImageUrl(show.PosterPath),
            ArtUrl = ImageUrl(show.BackdropPath),
            Images = BuildEpisodeImages(show, episode),
            ExternalIds =
            [
                new ExternalIdValue
                {
                    Provider = "tmdb",
                    Id = episode.Id?.ToString() ?? $"{show.Id}:{season.SeasonNumber}:{episode.EpisodeNumber}"
                }
            ],
            Cast = Cast((episode as TmdbEpisodeDetails)?.Credits?.Cast),
            Directors = Crew((episode as TmdbEpisodeDetails)?.Credits?.Crew, "Director"),
            Producers = Crew((episode as TmdbEpisodeDetails)?.Credits?.Crew, "Producer", "Executive Producer"),
            Writers = Crew((episode as TmdbEpisodeDetails)?.Credits?.Crew, "Writer", "Screenplay", "Story")
        };

    private static IReadOnlyList<ExtraMetadataModel> BuildExtras(IEnumerable<TmdbVideoResult>? videos, string? fallbackThumb, string? fallbackArt, DateOnly? fallbackDate)
    {
        var items = (videos ?? [])
            .Where(video => !string.IsNullOrWhiteSpace(video.Key) && !string.IsNullOrWhiteSpace(video.Name))
            .Select(video => new
            {
                Video = video,
                Subtype = MapSubtype(video.Type),
                SortOrder = SortOrder(video.Type),
                PublishedAt = ParsePublishedAt(video.PublishedAt)
            })
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Video.Official ? 0 : 1)
            .ThenByDescending(item => item.PublishedAt)
            .ThenBy(item => item.Video.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var results = new List<ExtraMetadataModel>(items.Count);
        var primaryAssigned = false;

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var isPrimary = !primaryAssigned && item.Subtype == "trailer";
            if (isPrimary)
            {
                primaryAssigned = true;
            }

            results.Add(new ExtraMetadataModel
            {
                SourceKey = "tmdb",
                SourceId = !string.IsNullOrWhiteSpace(item.Video.Id)
                    ? item.Video.Id!
                    : $"{item.Video.Site?.ToLowerInvariant()}:{item.Video.Key}",
                Title = item.Video.Name!,
                Subtype = item.Subtype,
                Summary = BuildSummary(item.Video),
                ThumbUrl = BuildVideoThumb(item.Video) ?? fallbackThumb,
                ArtUrl = fallbackArt,
                OriginallyAvailableAt = item.PublishedAt is DateTimeOffset publishedAt
                    ? DateOnly.FromDateTime(publishedAt.UtcDateTime)
                    : fallbackDate,
                DurationMilliseconds = null,
                Year = item.PublishedAt?.Year ?? fallbackDate?.Year,
                Index = index + 1,
                IsPrimary = isPrimary
            });
        }

        return results;
    }

    private static string MapSubtype(string? videoType)
        => videoType?.Trim() switch
        {
            "Trailer" => "trailer",
            "Teaser" => "trailer",
            "Behind the Scenes" => "behindTheScenes",
            "Interview" => "interview",
            "Featurette" => "featurette",
            "Clip" => "sceneOrSample",
            "Scene" => "sceneOrSample",
            "Deleted Scene" => "deletedScene",
            "Short" => "short",
            _ => "other"
        };

    private static int SortOrder(string? videoType)
        => videoType?.Trim() switch
        {
            "Trailer" => 0,
            "Teaser" => 1,
            "Behind the Scenes" => 2,
            "Interview" => 3,
            "Featurette" => 4,
            "Clip" => 5,
            "Scene" => 5,
            "Deleted Scene" => 6,
            "Short" => 7,
            _ => 8
        };

    private static string? BuildSummary(TmdbVideoResult video)
    {
        var site = NullIfWhiteSpace(video.Site);
        var type = NullIfWhiteSpace(video.Type);
        var published = ParsePublishedAt(video.PublishedAt)?.ToString("yyyy-MM-dd");

        var parts = new[] { type, site, published }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();

        return parts.Count == 0 ? null : string.Join(" • ", parts);
    }

    private static string? BuildVideoThumb(TmdbVideoResult video)
        => string.Equals(video.Site, "YouTube", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(video.Key)
            ? $"https://i.ytimg.com/vi/{video.Key}/hqdefault.jpg"
            : null;

    private static DateTimeOffset? ParsePublishedAt(string? value)
        => DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;

    private static IReadOnlyList<SourceImage> BuildShowImages(TmdbTvDetails show)
        => DistinctImages(
        [
            .. ImageGroup(show.Images?.Posters, "coverPoster", show.Name),
            .. ImageGroup(show.Images?.Backdrops, "background", show.Name),
            .. ImageGroup(show.Images?.Logos, "clearLogo", show.Name),
            .. FallbackImage(ImageUrl(show.PosterPath), "coverPoster", show.Name),
            .. FallbackImage(ImageUrl(show.BackdropPath), "background", show.Name)
        ]);

    private static IReadOnlyList<SourceImage> BuildSeasonImages(TmdbTvDetails show, TmdbSeasonSummary season)
        => DistinctImages(
        [
            .. ImageGroup(season.Images?.Posters, "coverPoster", season.Name),
            .. FallbackImage(ImageUrl(season.PosterPath), "coverPoster", season.Name),
            .. FallbackImage(ImageUrl(show.BackdropPath), "background", show.Name)
        ]);

    private static IReadOnlyList<SourceImage> BuildEpisodeImages(TmdbTvDetails show, TmdbEpisodeSummary episode)
        => DistinctImages(
        [
            .. ImageGroup((episode as TmdbEpisodeDetails)?.Images?.Stills, "snapshot", episode.Name),
            .. FallbackImage(ImageUrl(episode.StillPath), "snapshot", episode.Name),
            .. FallbackImage(ImageUrl(show.BackdropPath), "background", show.Name)
        ]);

    private static IReadOnlyList<ExternalIdValue> BuildExternalIds(int tmdbId, string? imdbId, int? tvdbId)
    {
        var items = new List<ExternalIdValue>
        {
            new() { Provider = "tmdb", Id = tmdbId.ToString() }
        };

        if (!string.IsNullOrWhiteSpace(imdbId))
        {
            items.Add(new ExternalIdValue { Provider = "imdb", Id = imdbId });
        }

        if (tvdbId.HasValue)
        {
            items.Add(new ExternalIdValue { Provider = "tvdb", Id = tvdbId.Value.ToString() });
        }

        return items;
    }

    private static IReadOnlyList<PersonCredit>? Cast(IEnumerable<TmdbCastMember>? items)
    {
        var cast = items?
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Take(20)
            .Select(item => new PersonCredit
            {
                Name = item.Name!,
                Role = NullIfWhiteSpace(item.Character ?? item.Roles?.FirstOrDefault()?.Character),
                Thumb = ImageUrl(item.ProfilePath, "w500"),
                Order = item.Order
            })
            .ToList();

        return cast is { Count: > 0 } ? cast : null;
    }

    private static IReadOnlyList<PersonCredit>? Crew(IEnumerable<TmdbCrewMember>? items, params string[] jobs)
    {
        var allowedJobs = jobs.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var crew = items?
            .Where(item => item.Name is not null && CrewMatches(item, allowedJobs))
            .Take(15)
            .Select(item => new PersonCredit
            {
                Name = item.Name!,
                Thumb = ImageUrl(item.ProfilePath, "w500")
            })
            .ToList();

        return crew is { Count: > 0 } ? crew : null;
    }

    private static bool CrewMatches(TmdbCrewMember item, HashSet<string> allowedJobs)
    {
        if (!string.IsNullOrWhiteSpace(item.Job) && allowedJobs.Contains(item.Job))
        {
            return true;
        }

        return item.Jobs?.Any(job => !string.IsNullOrWhiteSpace(job.Job) && allowedJobs.Contains(job.Job!)) == true;
    }

    private static IReadOnlyList<string>? Names(IEnumerable<TmdbNamedEntity>? items)
    {
        var values = items?
            .Select(item => NullIfWhiteSpace(item.Name))
            .Where(item => item is not null)
            .Select(item => item!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return values is { Count: > 0 } ? values : null;
    }

    private static string? FirstName(IEnumerable<TmdbNamedEntity>? items)
        => Names(items)?.FirstOrDefault();

    private static string? TvContentRating(TmdbTvDetails show, string country)
    {
        var rating = show.ContentRatings?.Results?
            .FirstOrDefault(item => string.Equals(item.Iso31661, country, StringComparison.OrdinalIgnoreCase))?.Rating;

        rating ??= show.ContentRatings?.Results?
            .FirstOrDefault(item => string.Equals(item.Iso31661, "US", StringComparison.OrdinalIgnoreCase))?.Rating;

        if (string.IsNullOrWhiteSpace(rating))
        {
            return null;
        }

        return string.Equals(country, "US", StringComparison.OrdinalIgnoreCase)
            ? rating
            : $"{country.ToLowerInvariant()}/{rating}";
    }

    private static IReadOnlyList<SourceImage> ImageGroup(IEnumerable<TmdbImageFile>? items, string type, string? alt)
        => items?
            .Select(item => ImageUrl(item.FilePath))
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => new SourceImage { Type = type, Url = url!, Alt = alt })
            .ToList()
           ?? [];

    private static IReadOnlyList<SourceImage> FallbackImage(string? url, string type, string? alt)
        => string.IsNullOrWhiteSpace(url)
            ? []
            : [new SourceImage { Type = type, Url = url, Alt = alt }];

    private static IReadOnlyList<SourceImage> DistinctImages(IEnumerable<SourceImage> images)
        => images.GroupBy(image => image.Url, StringComparer.OrdinalIgnoreCase).Select(group => group.First()).ToList();

    private static string? ImageUrl(string? path, string size = "original")
        => string.IsNullOrWhiteSpace(path) ? null : $"{ImageBase}/{size}{path}";

    private static DateOnly? ParseDate(string? value)
        => DateOnly.TryParse(value, out var parsed) ? parsed : null;

    private static int? FirstPositiveRuntime(IEnumerable<int>? runtimes)
        => runtimes?.FirstOrDefault(item => item > 0) is int runtime && runtime > 0 ? runtime : null;

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? DifferentOrNull(string? value, string? current)
        => string.Equals(value, current, StringComparison.OrdinalIgnoreCase) ? null : NullIfWhiteSpace(value);
}
