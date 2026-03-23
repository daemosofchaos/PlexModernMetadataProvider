using Microsoft.Extensions.Options;
using PlexModernMetadataProvider.Api.Models;
using PlexModernMetadataProvider.Api.Options;

namespace PlexModernMetadataProvider.Api.Services;

public interface IMovieMetadataSource
{
    string SourceKey { get; }
    bool IsEnabled { get; }
    Task<MovieMetadataModel?> FindByExternalGuidAsync(ExternalGuid guid, PlexRequestContext context, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovieSearchCandidate>> SearchAsync(string queryTitle, int? requestedYear, bool includeAdult, PlexRequestContext context, CancellationToken cancellationToken = default);
    Task<MovieMetadataModel?> GetByIdAsync(string sourceId, PlexRequestContext context, CancellationToken cancellationToken = default);
}

public interface ITvMetadataSource
{
    string SourceKey { get; }
    bool IsEnabled { get; }
    Task<ShowMetadataModel?> FindByExternalGuidAsync(ExternalGuid guid, PlexRequestContext context, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TvShowSearchCandidate>> SearchAsync(string queryTitle, int? requestedYear, PlexRequestContext context, CancellationToken cancellationToken = default);
    Task<ShowMetadataModel?> GetShowAsync(string sourceId, PlexRequestContext context, CancellationToken cancellationToken = default);
    Task<SeasonMetadataModel?> GetSeasonAsync(string sourceId, int seasonNumber, PlexRequestContext context, CancellationToken cancellationToken = default);
    Task<EpisodeMetadataModel?> GetEpisodeAsync(string sourceId, int seasonNumber, int episodeNumber, PlexRequestContext context, CancellationToken cancellationToken = default);
    Task<EpisodeMetadataModel?> GetEpisodeByAirDateAsync(string sourceId, DateOnly airDate, PlexRequestContext context, CancellationToken cancellationToken = default);
}

public sealed class MetadataSourceRegistry
{
    private readonly ProviderOptions _options;
    private readonly IReadOnlyDictionary<string, IMovieMetadataSource> _movieSources;
    private readonly IReadOnlyDictionary<string, ITvMetadataSource> _tvSources;

    public MetadataSourceRegistry(
        IEnumerable<IMovieMetadataSource> movieSources,
        IEnumerable<ITvMetadataSource> tvSources,
        IOptions<ProviderOptions> options)
    {
        _options = options.Value;
        _movieSources = BuildDictionary(movieSources.Where(source => source.IsEnabled), source => source.SourceKey);
        _tvSources = BuildDictionary(tvSources.Where(source => source.IsEnabled), source => source.SourceKey);
    }

    public IReadOnlyList<IMovieMetadataSource> GetMovieSources()
        => OrderSources(_options.MovieSourceOrder, _movieSources);

    public IReadOnlyList<ITvMetadataSource> GetTvSources()
        => OrderSources(_options.TvSourceOrder, _tvSources);

    public IMovieMetadataSource? GetMovieSource(string sourceKey)
        => _movieSources.TryGetValue(sourceKey, out var source) ? source : null;

    public ITvMetadataSource? GetTvSource(string sourceKey)
        => _tvSources.TryGetValue(sourceKey, out var source) ? source : null;

    private static IReadOnlyDictionary<string, T> BuildDictionary<T>(IEnumerable<T> items, Func<T, string> keySelector)
        where T : class
    {
        var dictionary = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            dictionary[keySelector(item)] = item;
        }

        return dictionary;
    }

    private static IReadOnlyList<T> OrderSources<T>(string configuredOrder, IReadOnlyDictionary<string, T> availableSources)
        where T : class
    {
        if (availableSources.Count == 0)
        {
            return [];
        }

        var remaining = new Dictionary<string, T>(availableSources, StringComparer.OrdinalIgnoreCase);
        var ordered = new List<T>(remaining.Count);

        foreach (var key in SplitOrder(configuredOrder))
        {
            if (remaining.Remove(key, out var source))
            {
                ordered.Add(source);
            }
        }

        ordered.AddRange(remaining.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => pair.Value));
        return ordered;
    }

    private static IEnumerable<string> SplitOrder(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(item => !string.IsNullOrWhiteSpace(item));
}
