using System.Net.Http.Headers;
using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PlexModernMetadataProvider.Api.Models;
using PlexModernMetadataProvider.Api.Options;

namespace PlexModernMetadataProvider.Api.Services;

public sealed record PlexExistingMatch(
    string Title,
    int? Year,
    string? PlexGuid,
    IReadOnlyList<ExternalIdValue> ExternalIds);

public interface IPlexReconciliationService
{
    bool IsEnabled { get; }
    Task<PlexExistingMatch?> FindExistingMovieAsync(string? title, int? year, CancellationToken cancellationToken = default);
    Task<PlexExistingMatch?> FindExistingShowAsync(string? title, int? year, CancellationToken cancellationToken = default);
}

public sealed class PlexReconciliationService : IPlexReconciliationService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly PlexOptions _options;
    private readonly ILogger<PlexReconciliationService> _logger;
    private readonly TimeSpan _cacheTtl;

    public PlexReconciliationService(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<ProviderOptions> options,
        ILogger<PlexReconciliationService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _options = options.Value.Plex;
        _cacheTtl = TimeSpan.FromMinutes(Math.Max(1, _options.CacheTtlMinutes));

        if (!string.IsNullOrWhiteSpace(_options.BaseUrl) && _httpClient.BaseAddress is null)
        {
            var normalized = _options.BaseUrl.EndsWith("/", StringComparison.Ordinal)
                ? _options.BaseUrl
                : _options.BaseUrl + "/";
            _httpClient.BaseAddress = new Uri(normalized, UriKind.Absolute);
        }

        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.RequestTimeoutSeconds));

        if (!_httpClient.DefaultRequestHeaders.Accept.Any(header => header.MediaType == "application/xml"))
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        }
    }

    public bool IsEnabled
        => _options.EnableReconciliation
           && !string.IsNullOrWhiteSpace(_options.BaseUrl)
           && !string.IsNullOrWhiteSpace(_options.Token);

    public Task<PlexExistingMatch?> FindExistingMovieAsync(string? title, int? year, CancellationToken cancellationToken = default)
        => FindExistingAsync(1, title, year, cancellationToken);

    public Task<PlexExistingMatch?> FindExistingShowAsync(string? title, int? year, CancellationToken cancellationToken = default)
        => FindExistingAsync(2, title, year, cancellationToken);

    private async Task<PlexExistingMatch?> FindExistingAsync(int plexType, string? title, int? year, CancellationToken cancellationToken)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var cacheKey = $"plex-reconcile:{plexType}:{NormalizeTitle(title)}:{year?.ToString() ?? "none"}";
        if (_cache.TryGetValue(cacheKey, out PlexExistingMatch? cached))
        {
            return cached;
        }

        var query = new Dictionary<string, string?>
        {
            ["includeGuids"] = "1",
            ["type"] = plexType.ToString(),
            ["title"] = title,
            ["limit"] = "25",
            ["X-Plex-Container-Start"] = "0",
            ["X-Plex-Container-Size"] = "25",
            ["X-Plex-Token"] = _options.Token
        };

        try
        {
            var document = await QueryXmlAsync(cacheKey + ":xml", "/library/all", query, cancellationToken);
            var candidates = ParseCandidates(document).ToList();
            var match = SelectBestMatch(candidates, title, year);
            _cache.Set(cacheKey, match, _cacheTtl);
            return match;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Plex reconciliation search failed for title '{Title}' year '{Year}'.", title, year);
            _cache.Set(cacheKey, null as PlexExistingMatch, TimeSpan.FromMinutes(1));
            return null;
        }
    }

    private async Task<XDocument> QueryXmlAsync(string cacheKey, string path, IReadOnlyDictionary<string, string?> query, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(cacheKey, out XDocument? cached) && cached is not null)
        {
            return cached;
        }

        var requestUri = BuildRequestUri(path, query);
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Plex reconciliation request to {RequestUri} failed with {StatusCode}: {Body}", requestUri, (int)response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }

        var xml = await response.Content.ReadAsStringAsync(cancellationToken);
        var document = XDocument.Parse(xml, LoadOptions.None);
        _cache.Set(cacheKey, document, _cacheTtl);
        return document;
    }

    private static string BuildRequestUri(string path, IReadOnlyDictionary<string, string?> query)
    {
        var queryString = string.Join("&", query
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}"));

        return string.IsNullOrWhiteSpace(queryString)
            ? path
            : $"{path}?{queryString}";
    }

    private static IEnumerable<PlexExistingMatch> ParseCandidates(XDocument document)
    {
        return document
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "Video", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(element.Name.LocalName, "Directory", StringComparison.OrdinalIgnoreCase))
            .Select(ParseCandidate)
            .Where(candidate => candidate is not null)
            .Cast<PlexExistingMatch>();
    }

    private static PlexExistingMatch? ParseCandidate(XElement element)
    {
        var title = Value(element, "title");
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var externalIds = new List<ExternalIdValue>();
        AddExternalId(externalIds, Value(element, "guid"));

        foreach (var guidElement in element.Elements().Where(item => string.Equals(item.Name.LocalName, "Guid", StringComparison.OrdinalIgnoreCase)))
        {
            AddExternalId(externalIds, guidElement.Attribute("id")?.Value);
        }

        return new PlexExistingMatch(
            title.Trim(),
            ParseYear(Value(element, "year"), Value(element, "originallyAvailableAt")),
            Value(element, "guid"),
            externalIds
                .Where(item => !string.IsNullOrWhiteSpace(item.Provider) && !string.IsNullOrWhiteSpace(item.Id))
                .DistinctBy(item => $"{item.Provider}:{item.Id}", StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    private static void AddExternalId(List<ExternalIdValue> externalIds, string? guid)
    {
        var parsed = RatingKeys.ParseExternalGuid(guid);
        if (parsed is null)
        {
            return;
        }

        if (!IsSupportedExternalProvider(parsed.Provider))
        {
            return;
        }

        externalIds.Add(new ExternalIdValue
        {
            Provider = parsed.Provider,
            Id = parsed.Id
        });
    }

    private static bool IsSupportedExternalProvider(string provider)
        => provider.ToLowerInvariant() switch
        {
            "tmdb" => true,
            "tvdb" => true,
            "imdb" => true,
            "tvmaze" => true,
            "omdb" => true,
            _ => false
        };

    public static PlexExistingMatch? SelectBestMatch(IEnumerable<PlexExistingMatch> candidates, string requestedTitle, int? requestedYear)
    {
        var exactTitleMatches = candidates
            .Where(candidate => string.Equals(NormalizeTitle(candidate.Title), NormalizeTitle(requestedTitle), StringComparison.Ordinal))
            .ToList();

        if (exactTitleMatches.Count == 0)
        {
            return null;
        }

        if (requestedYear.HasValue)
        {
            var yearMatches = exactTitleMatches
                .Where(candidate => candidate.Year == requestedYear.Value)
                .OrderByDescending(candidate => candidate.ExternalIds.Count)
                .ToList();

            return yearMatches.Count switch
            {
                0 => null,
                1 => yearMatches[0],
                _ => yearMatches[0]
            };
        }

        return exactTitleMatches.Count == 1
            ? exactTitleMatches[0]
            : null;
    }

    private static int? ParseYear(string? yearValue, string? dateValue)
    {
        if (int.TryParse(yearValue, out var parsedYear) && parsedYear > 0)
        {
            return parsedYear;
        }

        return DateOnly.TryParse(dateValue, out var parsedDate)
            ? parsedDate.Year
            : null;
    }

    private static string? Value(XElement element, string attributeName)
        => element.Attributes().FirstOrDefault(attribute => string.Equals(attribute.Name.LocalName, attributeName, StringComparison.OrdinalIgnoreCase))?.Value;

    private static string NormalizeTitle(string value)
    {
        var buffer = new char[value.Length];
        var index = 0;
        var previousWasSpace = true;

        foreach (var character in value.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                buffer[index++] = char.ToLowerInvariant(character);
                previousWasSpace = false;
                continue;
            }

            if (previousWasSpace)
            {
                continue;
            }

            buffer[index++] = ' ';
            previousWasSpace = true;
        }

        if (index > 0 && buffer[index - 1] == ' ')
        {
            index--;
        }

        return new string(buffer, 0, index);
    }
}
