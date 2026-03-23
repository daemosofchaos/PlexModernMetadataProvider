using Microsoft.Extensions.Options;
using PlexModernMetadataProvider.Api.Models;
using PlexModernMetadataProvider.Api.Options;

namespace PlexModernMetadataProvider.Api.Services;

public sealed class MatchService
{
    private readonly MetadataSourceRegistry _sourceRegistry;
    private readonly RankingService _rankingService;
    private readonly FilenameParser _filenameParser;
    private readonly PlexMapper _plexMapper;
    private readonly ProviderOptions _options;

    public MatchService(
        MetadataSourceRegistry sourceRegistry,
        RankingService rankingService,
        FilenameParser filenameParser,
        PlexMapper plexMapper,
        IOptions<ProviderOptions> options)
    {
        _sourceRegistry = sourceRegistry;
        _rankingService = rankingService;
        _filenameParser = filenameParser;
        _plexMapper = plexMapper;
        _options = options.Value;
    }

    public Task<MetadataResponse> MatchAsync(MatchRequest request, PlexRequestContext context, CancellationToken cancellationToken = default)
        => request.Type switch
        {
            1 => MatchMovieAsync(request, context, cancellationToken),
            2 => MatchShowAsync(request, context, cancellationToken),
            3 => MatchSeasonAsync(request, context, cancellationToken),
            4 => MatchEpisodeAsync(request, context, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported metadata type '{request.Type}'.")
        };

    private MetadataResponse Empty(string identifier) => _plexMapper.WrapMetadata(identifier, []);

    private int? RequestedYear(MatchRequest request)
    {
        if (request.Year.HasValue)
        {
            return request.Year.Value;
        }

        return _filenameParser.Parse(request.Filename).Year;
    }

    private string? RequestedTitle(MatchRequest request, string? fallbackTitle = null)
    {
        if (!string.IsNullOrWhiteSpace(fallbackTitle))
        {
            return fallbackTitle;
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            return request.Title;
        }

        return _filenameParser.Parse(request.Filename).Title;
    }

    private async Task<MovieMetadataModel?> ResolveMovieByExternalGuidAsync(MatchRequest request, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var external = RatingKeys.ParseExternalGuid(request.Guid);
        if (external is null)
        {
            return null;
        }

        foreach (var source in _sourceRegistry.GetMovieSources())
        {
            var result = await source.FindByExternalGuidAsync(external, context, cancellationToken);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private async Task<ShowMetadataModel?> ResolveTvByExternalGuidAsync(MatchRequest request, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var external = RatingKeys.ParseExternalGuid(request.Guid);
        if (external is null)
        {
            return null;
        }

        foreach (var source in _sourceRegistry.GetTvSources())
        {
            var result = await source.FindByExternalGuidAsync(external, context, cancellationToken);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private async Task<List<MovieSearchCandidate>> SearchMoviesAsync(MatchRequest request, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var title = RequestedTitle(request);
        if (string.IsNullOrWhiteSpace(title))
        {
            return [];
        }

        var year = RequestedYear(request);
        var includeAdult = request.IncludeAdult == 1;
        var results = new List<MovieSearchCandidate>();

        foreach (var source in _sourceRegistry.GetMovieSources())
        {
            var sourceResults = await source.SearchAsync(title, year, includeAdult, context, cancellationToken);
            results.AddRange(sourceResults);
        }

        return RankMovieCandidates(results, title, year);
    }

    private async Task<List<TvShowSearchCandidate>> SearchShowsAsync(MatchRequest request, PlexRequestContext context, string? titleOverride, CancellationToken cancellationToken)
    {
        var title = RequestedTitle(request, titleOverride);
        if (string.IsNullOrWhiteSpace(title))
        {
            return [];
        }

        var year = RequestedYear(request);
        var results = new List<TvShowSearchCandidate>();

        foreach (var source in _sourceRegistry.GetTvSources())
        {
            var sourceResults = await source.SearchAsync(title, year, context, cancellationToken);
            results.AddRange(sourceResults);
        }

        return RankShowCandidates(results, title, year);
    }

    private List<MovieSearchCandidate> RankMovieCandidates(IEnumerable<MovieSearchCandidate> candidates, string title, int? year)
    {
        var byId = candidates.ToDictionary(candidate => CandidateKey(candidate.SourceKey, candidate.SourceId), StringComparer.OrdinalIgnoreCase);
        var ranked = _rankingService.Rank(candidates.Select(candidate => new CandidateDescriptor<string>(
            CandidateKey(candidate.SourceKey, candidate.SourceId),
            candidate.Title,
            candidate.OriginalTitle,
            candidate.ReleaseDate,
            candidate.Popularity)), title, year);

        return ranked.Select(candidate => byId[candidate.Id]).ToList();
    }

    private List<TvShowSearchCandidate> RankShowCandidates(IEnumerable<TvShowSearchCandidate> candidates, string title, int? year)
    {
        var byId = candidates.ToDictionary(candidate => CandidateKey(candidate.SourceKey, candidate.SourceId), StringComparer.OrdinalIgnoreCase);
        var ranked = _rankingService.Rank(candidates.Select(candidate => new CandidateDescriptor<string>(
            CandidateKey(candidate.SourceKey, candidate.SourceId),
            candidate.Title,
            candidate.OriginalTitle,
            candidate.FirstAirDate,
            candidate.Popularity)), title, year);

        return ranked.Select(candidate => byId[candidate.Id]).ToList();
    }

    private async Task<MetadataResponse> MatchMovieAsync(MatchRequest request, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var byGuid = await ResolveMovieByExternalGuidAsync(request, context, cancellationToken);
        if (byGuid is not null)
        {
            return _plexMapper.WrapMetadata(ProviderDefinitions.MovieIdentifier, [_plexMapper.MapMovie(byGuid)]);
        }

        var candidates = await SearchMoviesAsync(request, context, cancellationToken);
        if (candidates.Count == 0)
        {
            return Empty(ProviderDefinitions.MovieIdentifier);
        }

        var limit = request.Manual == 1 ? _options.MaxManualMatches : 1;
        var matches = new List<object>();

        foreach (var candidate in candidates)
        {
            var source = _sourceRegistry.GetMovieSource(candidate.SourceKey);
            if (source is null)
            {
                continue;
            }

            var movie = await source.GetByIdAsync(candidate.SourceId, context, cancellationToken);
            if (movie is null)
            {
                continue;
            }

            if (request.IncludeAdult != 1 && movie.IsAdult == true)
            {
                continue;
            }

            matches.Add(_plexMapper.MapMovie(movie));
            if (matches.Count >= limit)
            {
                break;
            }
        }

        return matches.Count == 0
            ? Empty(ProviderDefinitions.MovieIdentifier)
            : _plexMapper.WrapMetadata(ProviderDefinitions.MovieIdentifier, matches);
    }

    private async Task<MetadataResponse> MatchShowAsync(MatchRequest request, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var byGuid = await ResolveTvByExternalGuidAsync(request, context, cancellationToken);
        if (byGuid is not null)
        {
            return _plexMapper.WrapMetadata(ProviderDefinitions.TvIdentifier, [_plexMapper.MapShow(byGuid, request.IncludeChildren == 1)]);
        }

        var candidates = await SearchShowsAsync(request, context, null, cancellationToken);
        if (candidates.Count == 0)
        {
            return Empty(ProviderDefinitions.TvIdentifier);
        }

        var limit = request.Manual == 1 ? _options.MaxManualMatches : 1;
        var matches = new List<object>();

        foreach (var candidate in candidates)
        {
            var source = _sourceRegistry.GetTvSource(candidate.SourceKey);
            if (source is null)
            {
                continue;
            }

            var show = await source.GetShowAsync(candidate.SourceId, context, cancellationToken);
            if (show is null)
            {
                continue;
            }

            if (request.IncludeAdult != 1 && show.IsAdult == true)
            {
                continue;
            }

            matches.Add(_plexMapper.MapShow(show, request.IncludeChildren == 1));
            if (matches.Count >= limit)
            {
                break;
            }
        }

        return matches.Count == 0
            ? Empty(ProviderDefinitions.TvIdentifier)
            : _plexMapper.WrapMetadata(ProviderDefinitions.TvIdentifier, matches);
    }

    private async Task<MetadataResponse> MatchSeasonAsync(MatchRequest request, PlexRequestContext context, CancellationToken cancellationToken)
    {
        if (!request.Index.HasValue)
        {
            return Empty(ProviderDefinitions.TvIdentifier);
        }

        var parentTitle = !string.IsNullOrWhiteSpace(request.ParentTitle)
            ? request.ParentTitle
            : _filenameParser.Parse(request.Filename).Title;

        var candidates = await SearchShowsAsync(request, context, parentTitle, cancellationToken);
        foreach (var candidate in candidates)
        {
            var source = _sourceRegistry.GetTvSource(candidate.SourceKey);
            if (source is null)
            {
                continue;
            }

            var show = await source.GetShowAsync(candidate.SourceId, context, cancellationToken);
            if (show is null)
            {
                continue;
            }

            var season = await source.GetSeasonAsync(candidate.SourceId, request.Index.Value, context, cancellationToken);
            if (season is null)
            {
                continue;
            }

            return _plexMapper.WrapMetadata(ProviderDefinitions.TvIdentifier, [_plexMapper.MapSeason(show, season, request.IncludeChildren == 1)]);
        }

        return Empty(ProviderDefinitions.TvIdentifier);
    }

    private async Task<MetadataResponse> MatchEpisodeAsync(MatchRequest request, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var grandparentTitle = !string.IsNullOrWhiteSpace(request.GrandparentTitle)
            ? request.GrandparentTitle
            : _filenameParser.Parse(request.Filename).Title;

        var candidates = await SearchShowsAsync(request, context, grandparentTitle, cancellationToken);
        if (candidates.Count == 0)
        {
            return Empty(ProviderDefinitions.TvIdentifier);
        }

        if (request.ParentIndex.HasValue && request.Index.HasValue)
        {
            foreach (var candidate in candidates)
            {
                var source = _sourceRegistry.GetTvSource(candidate.SourceKey);
                if (source is null)
                {
                    continue;
                }

                var show = await source.GetShowAsync(candidate.SourceId, context, cancellationToken);
                if (show is null)
                {
                    continue;
                }

                var season = await source.GetSeasonAsync(candidate.SourceId, request.ParentIndex.Value, context, cancellationToken);
                if (season is null)
                {
                    continue;
                }

                var episode = await source.GetEpisodeAsync(candidate.SourceId, request.ParentIndex.Value, request.Index.Value, context, cancellationToken);
                if (episode is null)
                {
                    continue;
                }

                return _plexMapper.WrapMetadata(ProviderDefinitions.TvIdentifier, [_plexMapper.MapEpisode(show, season, episode)]);
            }

            return Empty(ProviderDefinitions.TvIdentifier);
        }

        var parsedFilename = _filenameParser.Parse(request.Filename);
        var airDateValue = request.Date ?? parsedFilename.AirDate;
        if (!DateOnly.TryParse(airDateValue, out var airDate))
        {
            return Empty(ProviderDefinitions.TvIdentifier);
        }

        foreach (var candidate in candidates)
        {
            var source = _sourceRegistry.GetTvSource(candidate.SourceKey);
            if (source is null)
            {
                continue;
            }

            var show = await source.GetShowAsync(candidate.SourceId, context, cancellationToken);
            if (show is null)
            {
                continue;
            }

            var episode = await source.GetEpisodeByAirDateAsync(candidate.SourceId, airDate, context, cancellationToken);
            if (episode is null)
            {
                continue;
            }

            var season = await source.GetSeasonAsync(candidate.SourceId, episode.SeasonNumber, context, cancellationToken);
            if (season is null)
            {
                continue;
            }

            return _plexMapper.WrapMetadata(ProviderDefinitions.TvIdentifier, [_plexMapper.MapEpisode(show, season, episode)]);
        }

        return Empty(ProviderDefinitions.TvIdentifier);
    }

    private static string CandidateKey(string sourceKey, string sourceId)
        => $"{sourceKey}:{sourceId}";
}
