using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PlexModernMetadataProvider.Api.Models;
using PlexModernMetadataProvider.Api.Options;

namespace PlexModernMetadataProvider.Api.Services;

public interface ITmdbClient
{
    Task<TmdbSearchResponse<TmdbMovieSummary>> SearchMoviesAsync(string query, string language, int? year, bool includeAdult, CancellationToken cancellationToken = default);
    Task<TmdbSearchResponse<TmdbTvSummary>> SearchTvAsync(string query, string language, int? year, CancellationToken cancellationToken = default);
    Task<TmdbFindResponse> FindByExternalIdAsync(string externalId, string externalSource, string language, CancellationToken cancellationToken = default);
    Task<TmdbMovieDetails> GetMovieAsync(int movieId, string language, CancellationToken cancellationToken = default);
    Task<TmdbTvDetails> GetTvAsync(int tvId, string language, CancellationToken cancellationToken = default);
    Task<TmdbSeasonDetails> GetSeasonAsync(int tvId, int seasonNumber, string language, CancellationToken cancellationToken = default);
    Task<TmdbEpisodeDetails> GetEpisodeAsync(int tvId, int seasonNumber, int episodeNumber, string language, CancellationToken cancellationToken = default);
}

public sealed class TmdbClient : ITmdbClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TmdbClient> _logger;
    private readonly TmdbOptions _options;
    private readonly TimeSpan _cacheTtl;

    public TmdbClient(HttpClient httpClient, IMemoryCache cache, IOptions<ProviderOptions> options, ILogger<TmdbClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _options = options.Value.TMDb;
        _cacheTtl = TimeSpan.FromMinutes(Math.Max(1, _options.CacheTtlMinutes));

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        }

        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.RequestTimeoutSeconds));

        if (!_httpClient.DefaultRequestHeaders.Accept.Any(header => header.MediaType == "application/json"))
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        if (!string.IsNullOrWhiteSpace(_options.ReadAccessToken) && _httpClient.DefaultRequestHeaders.Authorization is null)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ReadAccessToken);
        }
    }

    public Task<TmdbSearchResponse<TmdbMovieSummary>> SearchMoviesAsync(string query, string language, int? year, bool includeAdult, CancellationToken cancellationToken = default)
        => GetAsync<TmdbSearchResponse<TmdbMovieSummary>>(
            $"search-movie:{query}:{language}:{year}:{includeAdult}",
            "search/movie",
            new Dictionary<string, string?>
            {
                ["query"] = query,
                ["language"] = language,
                ["include_adult"] = includeAdult ? "true" : "false",
                ["primary_release_year"] = year?.ToString()
            },
            cancellationToken);

    public Task<TmdbSearchResponse<TmdbTvSummary>> SearchTvAsync(string query, string language, int? year, CancellationToken cancellationToken = default)
        => GetAsync<TmdbSearchResponse<TmdbTvSummary>>(
            $"search-tv:{query}:{language}:{year}",
            "search/tv",
            new Dictionary<string, string?>
            {
                ["query"] = query,
                ["language"] = language,
                ["first_air_date_year"] = year?.ToString()
            },
            cancellationToken);

    public Task<TmdbFindResponse> FindByExternalIdAsync(string externalId, string externalSource, string language, CancellationToken cancellationToken = default)
        => GetAsync<TmdbFindResponse>(
            $"find:{externalSource}:{externalId}:{language}",
            $"find/{Uri.EscapeDataString(externalId)}",
            new Dictionary<string, string?>
            {
                ["external_source"] = externalSource,
                ["language"] = language
            },
            cancellationToken);

    public Task<TmdbMovieDetails> GetMovieAsync(int movieId, string language, CancellationToken cancellationToken = default)
        => GetAsync<TmdbMovieDetails>(
            $"movie:{movieId}:{language}",
            $"movie/{movieId}",
            new Dictionary<string, string?>
            {
                ["language"] = language,
                ["append_to_response"] = "credits,images,external_ids,release_dates,videos",
                ["include_image_language"] = "null,en"
            },
            cancellationToken);

    public Task<TmdbTvDetails> GetTvAsync(int tvId, string language, CancellationToken cancellationToken = default)
        => GetAsync<TmdbTvDetails>(
            $"tv:{tvId}:{language}",
            $"tv/{tvId}",
            new Dictionary<string, string?>
            {
                ["language"] = language,
                ["append_to_response"] = "aggregate_credits,images,external_ids,content_ratings,videos",
                ["include_image_language"] = "null,en"
            },
            cancellationToken);

    public Task<TmdbSeasonDetails> GetSeasonAsync(int tvId, int seasonNumber, string language, CancellationToken cancellationToken = default)
        => GetAsync<TmdbSeasonDetails>(
            $"season:{tvId}:{seasonNumber}:{language}",
            $"tv/{tvId}/season/{seasonNumber}",
            new Dictionary<string, string?>
            {
                ["language"] = language,
                ["append_to_response"] = "credits,images,external_ids,videos",
                ["include_image_language"] = "null,en"
            },
            cancellationToken);

    public Task<TmdbEpisodeDetails> GetEpisodeAsync(int tvId, int seasonNumber, int episodeNumber, string language, CancellationToken cancellationToken = default)
        => GetAsync<TmdbEpisodeDetails>(
            $"episode:{tvId}:{seasonNumber}:{episodeNumber}:{language}",
            $"tv/{tvId}/season/{seasonNumber}/episode/{episodeNumber}",
            new Dictionary<string, string?>
            {
                ["language"] = language,
                ["append_to_response"] = "credits,images,external_ids,videos",
                ["include_image_language"] = "null,en"
            },
            cancellationToken);

    private async Task<T> GetAsync<T>(string cacheKey, string path, Dictionary<string, string?> query, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(cacheKey, out T? cached) && cached is not null)
        {
            return cached;
        }

        if (string.IsNullOrWhiteSpace(_options.ReadAccessToken) && string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Provide Provider:TMDb:ReadAccessToken or Provider:TMDb:ApiKey.");
        }

        if (string.IsNullOrWhiteSpace(_options.ReadAccessToken))
        {
            query["api_key"] = _options.ApiKey;
        }

        var queryString = string.Join("&", query
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}"));

        var requestUri = string.IsNullOrWhiteSpace(queryString) ? path : $"{path}?{queryString}";

        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("TMDb request to {RequestUri} failed with {StatusCode}: {Body}", requestUri, (int)response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"TMDb response for '{requestUri}' was empty.");

        _cache.Set(cacheKey, payload, _cacheTtl);
        return payload;
    }
}
