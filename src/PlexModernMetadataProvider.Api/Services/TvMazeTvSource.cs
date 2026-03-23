using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PlexModernMetadataProvider.Api.Models;
using PlexModernMetadataProvider.Api.Options;

namespace PlexModernMetadataProvider.Api.Services;

public sealed partial class TvMazeTvSource : ITvMetadataSource
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly TvMazeOptions _options;
    private readonly ILogger<TvMazeTvSource> _logger;
    private readonly TimeSpan _cacheTtl;

    public TvMazeTvSource(HttpClient httpClient, IMemoryCache cache, IOptions<ProviderOptions> options, ILogger<TvMazeTvSource> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _options = options.Value.TVMaze;
        _cacheTtl = TimeSpan.FromMinutes(Math.Max(1, _options.CacheTtlMinutes));

        _httpClient.BaseAddress ??= new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.RequestTimeoutSeconds));
    }

    public string SourceKey => "tvmaze";
    public bool IsEnabled => true;

    public async Task<ShowMetadataModel?> FindByExternalGuidAsync(ExternalGuid guid, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (guid.Provider == "tvmaze" && int.TryParse(guid.Id, out var tvMazeId))
        {
            return await GetShowAsync(tvMazeId.ToString(), context, cancellationToken);
        }

        if (guid.Provider is not ("imdb" or "tvdb"))
        {
            return null;
        }

        var parameterName = guid.Provider == "imdb" ? "imdb" : "thetvdb";
        var show = await GetAsync<TvMazeShow?>($"lookup:{parameterName}:{guid.Id}", $"lookup/shows?{parameterName}={Uri.EscapeDataString(guid.Id)}", cancellationToken);
        return show is null ? null : await MapShowAsync(show, includeSeasonEpisodes: false, cancellationToken);
    }

    public async Task<IReadOnlyList<TvShowSearchCandidate>> SearchAsync(string queryTitle, int? requestedYear, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queryTitle))
        {
            return [];
        }

        var results = await GetAsync<List<TvMazeSearchResult>>(
            $"search:{queryTitle}",
            $"search/shows?q={Uri.EscapeDataString(queryTitle)}",
            cancellationToken);

        return results
            .Where(result => result.Show is not null)
            .Select(result => result.Show!)
            .Where(show => !requestedYear.HasValue || ParseDate(show.Premiered)?.Year == requestedYear.Value)
            .Select(show => new TvShowSearchCandidate
            {
                SourceKey = SourceKey,
                SourceId = show.Id.ToString(),
                Title = show.Name,
                OriginalTitle = null,
                FirstAirDate = ParseDate(show.Premiered),
                Popularity = 0,
                IsAdult = null
            })
            .ToList();
    }

    public async Task<ShowMetadataModel?> GetShowAsync(string sourceId, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(sourceId, out var showId))
        {
            return null;
        }

        var show = await GetAsync<TvMazeShow>($"show:{showId}", $"shows/{showId}", cancellationToken);
        return await MapShowAsync(show, includeSeasonEpisodes: false, cancellationToken);
    }

    public async Task<SeasonMetadataModel?> GetSeasonAsync(string sourceId, int seasonNumber, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(sourceId, out var showId))
        {
            return null;
        }

        var show = await GetAsync<TvMazeShow>($"show:{showId}", $"shows/{showId}", cancellationToken);
        var episodes = await GetEpisodesAsync(showId, cancellationToken);
        return BuildSeason(show, episodes, seasonNumber, includeEpisodes: true);
    }

    public async Task<EpisodeMetadataModel?> GetEpisodeAsync(string sourceId, int seasonNumber, int episodeNumber, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(sourceId, out var showId))
        {
            return null;
        }

        var show = await GetAsync<TvMazeShow>($"show:{showId}", $"shows/{showId}", cancellationToken);
        var episodes = await GetEpisodesAsync(showId, cancellationToken);
        var episode = episodes.FirstOrDefault(item => item.Season == seasonNumber && item.Number == episodeNumber);
        return episode is null ? null : BuildEpisode(show, episode);
    }

    public async Task<EpisodeMetadataModel?> GetEpisodeByAirDateAsync(string sourceId, DateOnly airDate, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(sourceId, out var showId))
        {
            return null;
        }

        var show = await GetAsync<TvMazeShow>($"show:{showId}", $"shows/{showId}", cancellationToken);
        var episodes = await GetEpisodesAsync(showId, cancellationToken);
        var episode = episodes.FirstOrDefault(item => ParseDate(item.AirDate) == airDate);
        return episode is null ? null : BuildEpisode(show, episode);
    }

    private async Task<ShowMetadataModel> MapShowAsync(TvMazeShow show, bool includeSeasonEpisodes, CancellationToken cancellationToken)
    {
        var episodes = await GetEpisodesAsync(show.Id, cancellationToken);
        var cast = await GetCastAsync(show.Id, cancellationToken);
        var seasons = episodes
            .Where(item => item.Season >= 0)
            .Select(item => item.Season)
            .Distinct()
            .OrderBy(item => item)
            .Select(seasonNumber => BuildSeason(show, episodes, seasonNumber, includeSeasonEpisodes))
            .Where(season => season is not null)
            .Cast<SeasonMetadataModel>()
            .ToList();

        return new ShowMetadataModel
        {
            SourceKey = SourceKey,
            SourceId = show.Id.ToString(),
            Title = show.Name ?? $"Show {show.Id}",
            OriginalTitle = null,
            FirstAirDate = ParseDate(show.Premiered),
            Summary = StripHtml(show.Summary),
            IsAdult = null,
            RuntimeMinutes = show.AverageRuntime ?? show.Runtime,
            Tagline = null,
            Studio = show.Network?.Name ?? show.WebChannel?.Name,
            Rating = show.Rating?.Average > 0 ? Math.Round(show.Rating.Average.Value, 1) : null,
            RatingImage = "tvmaze://image.rating",
            ContentRating = null,
            ThumbUrl = BestImage(show.Image),
            ArtUrl = BestImage(show.Image),
            Images = BuildShowImages(show),
            Genres = show.Genres?.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            Countries = Countries(show),
            Studios = (show.Network?.Name ?? show.WebChannel?.Name) is { } studio ? new[] { studio } : null,
            Networks = (show.Network?.Name ?? show.WebChannel?.Name) is { } network ? new[] { network } : null,
            ExternalIds = BuildExternalIds(show),
            Cast = cast,
            Directors = null,
            Producers = null,
            Writers = null,
            Seasons = seasons
        };
    }

    private static SeasonMetadataModel? BuildSeason(TvMazeShow show, IReadOnlyList<TvMazeEpisode> episodes, int seasonNumber, bool includeEpisodes)
    {
        var seasonEpisodes = episodes
            .Where(item => item.Season == seasonNumber)
            .OrderBy(item => item.Number ?? int.MaxValue)
            .ToList();

        if (seasonEpisodes.Count == 0)
        {
            return null;
        }

        return new SeasonMetadataModel
        {
            SourceKey = "tvmaze",
            ShowSourceId = show.Id.ToString(),
            SeasonNumber = seasonNumber,
            Title = seasonNumber == 0 ? "Specials" : $"Season {seasonNumber}",
            AirDate = seasonEpisodes.Select(item => ParseDate(item.AirDate)).FirstOrDefault(item => item.HasValue),
            Summary = null,
            ThumbUrl = BestImage(show.Image),
            ArtUrl = BestImage(show.Image),
            Images = BuildSeasonImages(show),
            ExternalIds =
            [
                new ExternalIdValue { Provider = "tvmaze", Id = $"{show.Id}:{seasonNumber}" }
            ],
            Episodes = includeEpisodes ? seasonEpisodes.Select(item => BuildEpisode(show, item)).ToList() : null
        };
    }

    private static EpisodeMetadataModel BuildEpisode(TvMazeShow show, TvMazeEpisode episode)
        => new()
        {
            SourceKey = "tvmaze",
            ShowSourceId = show.Id.ToString(),
            SeasonNumber = episode.Season,
            EpisodeNumber = episode.Number ?? 0,
            Title = episode.Name ?? $"Episode {episode.Number ?? 0}",
            AirDate = ParseDate(episode.AirDate),
            Summary = StripHtml(episode.Summary),
            RuntimeMinutes = episode.Runtime ?? show.AverageRuntime ?? show.Runtime,
            Rating = episode.Rating?.Average > 0 ? Math.Round(episode.Rating.Average.Value, 1) : null,
            RatingImage = "tvmaze://image.rating",
            ThumbUrl = BestImage(episode.Image) ?? BestImage(show.Image),
            ArtUrl = BestImage(show.Image),
            Images = BuildEpisodeImages(show, episode),
            ExternalIds =
            [
                new ExternalIdValue { Provider = "tvmaze", Id = episode.Id.ToString() }
            ],
            Cast = null,
            Directors = null,
            Producers = null,
            Writers = null
        };

    private async Task<IReadOnlyList<TvMazeEpisode>> GetEpisodesAsync(int showId, CancellationToken cancellationToken)
        => await GetAsync<List<TvMazeEpisode>>($"episodes:{showId}", $"shows/{showId}/episodes?specials=1", cancellationToken);

    private async Task<IReadOnlyList<PersonCredit>?> GetCastAsync(int showId, CancellationToken cancellationToken)
    {
        var castEntries = await GetAsync<List<TvMazeCastEntry>>($"cast:{showId}", $"shows/{showId}/cast", cancellationToken);
        var cast = castEntries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Person?.Name))
            .Take(20)
            .Select((entry, index) => new PersonCredit
            {
                Name = entry.Person!.Name!,
                Role = entry.Character?.Name,
                Thumb = BestImage(entry.Person.Image),
                Order = index
            })
            .ToList();

        return cast.Count == 0 ? null : cast;
    }

    private async Task<T> GetAsync<T>(string cacheKey, string requestUri, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(cacheKey, out T? cached) && cached is not null)
        {
            return cached;
        }

        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("TVMaze request to {RequestUri} failed with {StatusCode}: {Body}", requestUri, (int)response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"TVMaze response for '{requestUri}' was empty.");

        _cache.Set(cacheKey, payload, _cacheTtl);
        return payload;
    }

    private static IReadOnlyList<ExternalIdValue> BuildExternalIds(TvMazeShow show)
    {
        var values = new List<ExternalIdValue>
        {
            new() { Provider = "tvmaze", Id = show.Id.ToString() }
        };

        if (!string.IsNullOrWhiteSpace(show.Externals?.Imdb))
        {
            values.Add(new ExternalIdValue { Provider = "imdb", Id = show.Externals.Imdb });
        }

        if (show.Externals?.TheTvDb is int tvdbId)
        {
            values.Add(new ExternalIdValue { Provider = "tvdb", Id = tvdbId.ToString() });
        }

        return values;
    }

    private static IReadOnlyList<string>? Countries(TvMazeShow show)
    {
        var countryNames = new[] { show.Network?.Country?.Name, show.WebChannel?.Country?.Name }
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return countryNames.Count == 0 ? null : countryNames;
    }

    private static IReadOnlyList<SourceImage>? BuildShowImages(TvMazeShow show)
    {
        var image = BestImage(show.Image);
        return image is null ? null : [new SourceImage { Type = "coverPoster", Url = image, Alt = show.Name }];
    }

    private static IReadOnlyList<SourceImage>? BuildSeasonImages(TvMazeShow show)
    {
        var image = BestImage(show.Image);
        return image is null ? null : [new SourceImage { Type = "coverPoster", Url = image, Alt = show.Name }];
    }

    private static IReadOnlyList<SourceImage>? BuildEpisodeImages(TvMazeShow show, TvMazeEpisode episode)
    {
        var images = new List<SourceImage>();
        var still = BestImage(episode.Image);
        var showImage = BestImage(show.Image);

        if (!string.IsNullOrWhiteSpace(still))
        {
            images.Add(new SourceImage { Type = "snapshot", Url = still, Alt = episode.Name });
        }

        if (!string.IsNullOrWhiteSpace(showImage))
        {
            images.Add(new SourceImage { Type = "background", Url = showImage, Alt = show.Name });
        }

        return images.Count == 0 ? null : images;
    }

    private static string? BestImage(TvMazeImage? image)
        => image?.Original ?? image?.Medium;

    private static DateOnly? ParseDate(string? value)
        => DateOnly.TryParse(value, out var parsed) ? parsed : null;

    private static string? StripHtml(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : WhiteSpaceRegex().Replace(HtmlRegex().Replace(value, " "), " ").Trim();

    [GeneratedRegex("<.*?>", RegexOptions.Compiled)]
    private static partial Regex HtmlRegex();

    [GeneratedRegex("\\s+", RegexOptions.Compiled)]
    private static partial Regex WhiteSpaceRegex();
}
