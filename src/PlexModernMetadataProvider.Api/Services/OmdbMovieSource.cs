using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PlexModernMetadataProvider.Api.Models;
using PlexModernMetadataProvider.Api.Options;

namespace PlexModernMetadataProvider.Api.Services;

public sealed class OmdbMovieSource : IMovieMetadataSource
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly OmdbOptions _options;
    private readonly ILogger<OmdbMovieSource> _logger;
    private readonly TimeSpan _cacheTtl;

    public OmdbMovieSource(HttpClient httpClient, IMemoryCache cache, IOptions<ProviderOptions> options, ILogger<OmdbMovieSource> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _options = options.Value.OMDb;
        _cacheTtl = TimeSpan.FromMinutes(Math.Max(1, _options.CacheTtlMinutes));

        _httpClient.BaseAddress ??= new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.RequestTimeoutSeconds));
    }

    public string SourceKey => "omdb";
    public bool IsEnabled => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<MovieMetadataModel?> FindByExternalGuidAsync(ExternalGuid guid, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || guid.Provider != "imdb")
        {
            return null;
        }

        return await GetByIdAsync(guid.Id, context, cancellationToken);
    }

    public async Task<IReadOnlyList<MovieSearchCandidate>> SearchAsync(string queryTitle, int? requestedYear, bool includeAdult, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(queryTitle))
        {
            return [];
        }

        var response = await GetAsync<OmdbSearchResponse>(
            $"search:{queryTitle}:{requestedYear}",
            new Dictionary<string, string?>
            {
                ["apikey"] = _options.ApiKey,
                ["s"] = queryTitle,
                ["type"] = "movie",
                ["y"] = requestedYear?.ToString()
            },
            cancellationToken);

        if (!string.Equals(response.Response, "True", StringComparison.OrdinalIgnoreCase) || response.Search is null)
        {
            return [];
        }

        return response.Search
            .Where(item => !string.IsNullOrWhiteSpace(item.ImdbId))
            .Select(item => new MovieSearchCandidate
            {
                SourceKey = SourceKey,
                SourceId = item.ImdbId!,
                Title = item.Title,
                OriginalTitle = null,
                ReleaseDate = ParseYear(item.Year),
                Popularity = 0,
                IsAdult = null
            })
            .ToList();
    }

    public async Task<MovieMetadataModel?> GetByIdAsync(string sourceId, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(sourceId))
        {
            return null;
        }

        var response = await GetAsync<OmdbMovieResponse>(
            $"movie:{sourceId}",
            new Dictionary<string, string?>
            {
                ["apikey"] = _options.ApiKey,
                ["i"] = sourceId,
                ["plot"] = "full",
                ["r"] = "json"
            },
            cancellationToken);

        if (!string.Equals(response.Response, "True", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var posterUrl = ValidValue(response.Poster);
        var studio = ValidValue(response.Production);
        var imdbId = ValidValue(response.ImdbId) ?? sourceId;
        var rating = ParseDouble(response.ImdbRating);
        var genres = SplitList(response.Genre);
        var countries = SplitList(response.Country);
        var actors = SplitPeople(response.Actors, role: null);
        var directors = SplitPeople(response.Director, role: null);
        var writers = SplitPeople(response.Writer, role: null);
        var studios = studio is null ? null : new[] { studio };

        return new MovieMetadataModel
        {
            SourceKey = SourceKey,
            SourceId = imdbId,
            Title = ValidValue(response.Title) ?? imdbId,
            OriginalTitle = null,
            ReleaseDate = ParseDate(response.Released) ?? ParseYear(response.Year),
            Summary = ValidValue(response.Plot),
            IsAdult = null,
            RuntimeMinutes = ParseRuntimeMinutes(response.Runtime),
            Tagline = null,
            Studio = studio,
            Rating = rating,
            RatingImage = "imdb://image.rating",
            ContentRating = ValidValue(response.Rated),
            ThumbUrl = posterUrl,
            ArtUrl = posterUrl,
            Images = posterUrl is null ? null : [new SourceImage { Type = "coverPoster", Url = posterUrl, Alt = response.Title }],
            Genres = genres,
            Countries = countries,
            Studios = studios,
            ExternalIds =
            [
                new ExternalIdValue { Provider = "omdb", Id = imdbId },
                new ExternalIdValue { Provider = "imdb", Id = imdbId }
            ],
            Cast = actors,
            Directors = directors,
            Producers = null,
            Writers = writers
        };
    }

    private async Task<T> GetAsync<T>(string cacheKey, Dictionary<string, string?> query, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(cacheKey, out T? cached) && cached is not null)
        {
            return cached;
        }

        var queryString = string.Join("&", query
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}"));

        var requestUri = string.IsNullOrWhiteSpace(queryString) ? string.Empty : $"?{queryString}";
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("OMDb request to {RequestUri} failed with {StatusCode}: {Body}", requestUri, (int)response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"OMDb response for '{requestUri}' was empty.");

        _cache.Set(cacheKey, payload, _cacheTtl);
        return payload;
    }

    private static DateOnly? ParseDate(string? value)
        => DateOnly.TryParse(value, out var parsed) ? parsed : null;

    private static DateOnly? ParseYear(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.TakeWhile(char.IsDigit).ToArray());
        return digits.Length == 4 && int.TryParse(digits, out var year)
            ? new DateOnly(year, 1, 1)
            : null;
    }

    private static int? ParseRuntimeMinutes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var runtime) && runtime > 0 ? runtime : null;
    }

    private static double? ParseDouble(string? value)
        => double.TryParse(ValidValue(value), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? Math.Round(parsed, 1)
            : null;

    private static string? ValidValue(string? value)
        => string.IsNullOrWhiteSpace(value) || string.Equals(value, "N/A", StringComparison.OrdinalIgnoreCase)
            ? null
            : value.Trim();

    private static IReadOnlyList<string>? SplitList(string? value)
    {
        var list = ValidValue(value)?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return list is { Count: > 0 } ? list : null;
    }

    private static IReadOnlyList<PersonCredit>? SplitPeople(string? value, string? role)
    {
        var people = ValidValue(value)?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(item => new PersonCredit { Name = item, Role = role })
            .ToList();

        return people is { Count: > 0 } ? people : null;
    }
}
