using PlexModernMetadataProvider.Api.Models;

namespace PlexModernMetadataProvider.Api.Services;

public sealed class MetadataService
{
    private readonly MetadataSourceRegistry _sourceRegistry;
    private readonly PlexMapper _plexMapper;
    private readonly TmdbMovieSource _tmdbMovieSource;
    private readonly TmdbTvSource _tmdbTvSource;

    public MetadataService(
        MetadataSourceRegistry sourceRegistry,
        PlexMapper plexMapper,
        TmdbMovieSource tmdbMovieSource,
        TmdbTvSource tmdbTvSource)
    {
        _sourceRegistry = sourceRegistry;
        _plexMapper = plexMapper;
        _tmdbMovieSource = tmdbMovieSource;
        _tmdbTvSource = tmdbTvSource;
    }

    public async Task<MetadataResponse> GetMovieMetadataAsync(string ratingKey, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        return RatingKeys.Parse(ratingKey) switch
        {
            MovieRatingKey movieKey => await GetMovieMetadataAsync(movieKey, context, cancellationToken),
            MovieExtraRatingKey movieExtraKey => await GetMovieExtraMetadataAsync(movieExtraKey, context, cancellationToken),
            _ => throw new InvalidOperationException("Movie metadata endpoint can only serve movie and movie-extra keys.")
        };
    }

    public async Task<MetadataResponse> GetMovieImagesAsync(string ratingKey, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        return RatingKeys.Parse(ratingKey) switch
        {
            MovieRatingKey movieKey => await GetMovieImagesAsync(movieKey, context, cancellationToken),
            MovieExtraRatingKey movieExtraKey => await GetMovieExtraImagesAsync(movieExtraKey, context, cancellationToken),
            _ => throw new InvalidOperationException("Movie image endpoint can only serve movie and movie-extra keys.")
        };
    }

    public async Task<MetadataResponse> GetMovieExtrasAsync(string ratingKey, PlexRequestContext context, PagingOptions paging, CancellationToken cancellationToken = default)
    {
        try
        {
            if (RatingKeys.Parse(ratingKey) is not MovieRatingKey movieKey)
            {
                return EmptyExtrasResponse(ProviderDefinitions.MovieIdentifier, paging);
            }

            var movie = await LoadMovieAsync(movieKey, context, cancellationToken);
            var extras = await ResolveMovieExtrasAsync(movieKey, movie, context, cancellationToken);
            return BuildMovieExtrasResponse(movie, extras, paging);
        }
        catch
        {
            return EmptyExtrasResponse(ProviderDefinitions.MovieIdentifier, paging);
        }
    }

    public async Task<MetadataResponse> GetTvMetadataAsync(string ratingKey, PlexRequestContext context, bool includeChildren, CancellationToken cancellationToken = default)
    {
        return RatingKeys.Parse(ratingKey) switch
        {
            ShowRatingKey showKey => await GetShowMetadataAsync(showKey, includeChildren, context, cancellationToken),
            SeasonRatingKey seasonKey => await GetSeasonMetadataAsync(seasonKey, includeChildren, context, cancellationToken),
            EpisodeRatingKey episodeKey => await GetEpisodeMetadataAsync(episodeKey, context, cancellationToken),
            ShowExtraRatingKey showExtraKey => await GetShowExtraMetadataAsync(showExtraKey, context, cancellationToken),
            SeasonExtraRatingKey seasonExtraKey => await GetSeasonExtraMetadataAsync(seasonExtraKey, context, cancellationToken),
            EpisodeExtraRatingKey episodeExtraKey => await GetEpisodeExtraMetadataAsync(episodeExtraKey, context, cancellationToken),
            _ => throw new InvalidOperationException("TV metadata endpoint can only serve show, season, episode, and TV extra keys.")
        };
    }

    public async Task<MetadataResponse> GetTvImagesAsync(string ratingKey, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        return RatingKeys.Parse(ratingKey) switch
        {
            ShowRatingKey showKey => await GetShowImagesAsync(showKey, context, cancellationToken),
            SeasonRatingKey seasonKey => await GetSeasonImagesAsync(seasonKey, context, cancellationToken),
            EpisodeRatingKey episodeKey => await GetEpisodeImagesAsync(episodeKey, context, cancellationToken),
            ShowExtraRatingKey showExtraKey => await GetShowExtraImagesAsync(showExtraKey, context, cancellationToken),
            SeasonExtraRatingKey seasonExtraKey => await GetSeasonExtraImagesAsync(seasonExtraKey, context, cancellationToken),
            EpisodeExtraRatingKey episodeExtraKey => await GetEpisodeExtraImagesAsync(episodeExtraKey, context, cancellationToken),
            _ => throw new InvalidOperationException("TV image endpoint can only serve show, season, episode, and TV extra keys.")
        };
    }

    public async Task<MetadataResponse> GetTvExtrasAsync(string ratingKey, PlexRequestContext context, PagingOptions paging, CancellationToken cancellationToken = default)
    {
        try
        {
            switch (RatingKeys.Parse(ratingKey))
            {
                case ShowRatingKey showKey:
                {
                    var show = await LoadShowAsync(showKey, context, cancellationToken);
                    var extras = await ResolveShowExtrasAsync(showKey, show, context, cancellationToken);
                    return BuildShowExtrasResponse(show, extras, paging);
                }
                case SeasonRatingKey seasonKey:
                {
                    var (show, season) = await LoadSeasonContextAsync(seasonKey, context, cancellationToken);
                    var extras = await ResolveSeasonExtrasAsync(seasonKey, show, context, cancellationToken);
                    return BuildSeasonExtrasResponse(show, season, extras, paging);
                }
                case EpisodeRatingKey episodeKey:
                {
                    var (show, season, episode) = await LoadEpisodeContextAsync(episodeKey, context, cancellationToken);
                    var extras = await ResolveEpisodeExtrasAsync(episodeKey, show, context, cancellationToken);
                    return BuildEpisodeExtrasResponse(show, season, episode, extras, paging);
                }
                default:
                    return EmptyExtrasResponse(ProviderDefinitions.TvIdentifier, paging);
            }
        }
        catch
        {
            return EmptyExtrasResponse(ProviderDefinitions.TvIdentifier, paging);
        }
    }

    public async Task<MetadataResponse> GetTvChildrenAsync(string ratingKey, PlexRequestContext context, PagingOptions paging, CancellationToken cancellationToken = default)
    {
        var skip = Math.Max(0, paging.ContainerStart - 1);
        var take = Math.Max(1, paging.ContainerSize);

        switch (RatingKeys.Parse(ratingKey))
        {
            case ShowRatingKey showKey:
            {
                var source = _sourceRegistry.GetTvSource(showKey.SourceKey)
                    ?? throw new InvalidOperationException($"TV source '{showKey.SourceKey}' is not enabled.");
                var show = await source.GetShowAsync(showKey.SourceId, context, cancellationToken)
                    ?? throw new InvalidOperationException("Show metadata not found.");

                var seasons = (show.Seasons ?? [])
                    .Select(season => (object)_plexMapper.MapSeason(show, season, includeChildren: false))
                    .ToList();

                return new MetadataResponse
                {
                    MediaContainer = new MediaContainer
                    {
                        Offset = skip,
                        TotalSize = seasons.Count,
                        Identifier = ProviderDefinitions.TvIdentifier,
                        Size = seasons.Skip(skip).Take(take).Count(),
                        Metadata = seasons.Skip(skip).Take(take).ToList()
                    }
                };
            }
            case SeasonRatingKey seasonKey:
            {
                var source = _sourceRegistry.GetTvSource(seasonKey.SourceKey)
                    ?? throw new InvalidOperationException($"TV source '{seasonKey.SourceKey}' is not enabled.");
                var show = await source.GetShowAsync(seasonKey.SourceId, context, cancellationToken)
                    ?? throw new InvalidOperationException("Show metadata not found.");
                var season = await source.GetSeasonAsync(seasonKey.SourceId, seasonKey.SeasonNumber, context, cancellationToken)
                    ?? throw new InvalidOperationException("Season metadata not found.");

                var episodes = (season.Episodes ?? [])
                    .Select(episode => (object)_plexMapper.MapEpisode(show, season, episode))
                    .ToList();

                return new MetadataResponse
                {
                    MediaContainer = new MediaContainer
                    {
                        Offset = skip,
                        TotalSize = episodes.Count,
                        Identifier = ProviderDefinitions.TvIdentifier,
                        Size = episodes.Skip(skip).Take(take).Count(),
                        Metadata = episodes.Skip(skip).Take(take).ToList()
                    }
                };
            }
            default:
                throw new InvalidOperationException("Children endpoint can only serve show and season keys.");
        }
    }

    private async Task<MetadataResponse> GetMovieMetadataAsync(MovieRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var movie = await LoadMovieAsync(key, context, cancellationToken);
        var extras = await ResolveMovieExtrasAsync(key, movie, context, cancellationToken);
        var primaryExtraKey = extras.FirstOrDefault(item => item.IsPrimary) is { } primary
            ? RatingKeys.BuildMetadataKey(ProviderDefinitions.MovieBasePath, RatingKeys.BuildMovieExtra(key.SourceKey, key.SourceId, primary.Index))
            : null;

        return _plexMapper.WrapMetadata(ProviderDefinitions.MovieIdentifier, [_plexMapper.MapMovie(movie, primaryExtraKey)]);
    }

    private async Task<MetadataResponse> GetMovieExtraMetadataAsync(MovieExtraRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var movieKey = new MovieRatingKey(key.SourceKey, key.SourceId);
        var movie = await LoadMovieAsync(movieKey, context, cancellationToken);
        var extras = await ResolveMovieExtrasAsync(movieKey, movie, context, cancellationToken);
        var extra = extras.FirstOrDefault(item => item.Index == key.ExtraIndex)
            ?? throw new InvalidOperationException("Movie extra metadata not found.");

        return _plexMapper.WrapMetadata(ProviderDefinitions.MovieIdentifier, [_plexMapper.MapMovieExtra(movie, extra)]);
    }

    private async Task<MetadataResponse> GetMovieImagesAsync(MovieRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var movie = await LoadMovieAsync(key, context, cancellationToken);
        return _plexMapper.WrapImages(ProviderDefinitions.MovieIdentifier, _plexMapper.MovieImages(movie) ?? []);
    }

    private async Task<MetadataResponse> GetMovieExtraImagesAsync(MovieExtraRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var movieKey = new MovieRatingKey(key.SourceKey, key.SourceId);
        var movie = await LoadMovieAsync(movieKey, context, cancellationToken);
        var extras = await ResolveMovieExtrasAsync(movieKey, movie, context, cancellationToken);
        var extra = extras.FirstOrDefault(item => item.Index == key.ExtraIndex)
            ?? throw new InvalidOperationException("Movie extra images not found.");

        return _plexMapper.WrapImages(ProviderDefinitions.MovieIdentifier, _plexMapper.MovieExtraImages(extra) ?? []);
    }

    private async Task<MovieMetadataModel> LoadMovieAsync(MovieRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var source = _sourceRegistry.GetMovieSource(key.SourceKey)
            ?? throw new InvalidOperationException($"Movie source '{key.SourceKey}' is not enabled.");

        return await source.GetByIdAsync(key.SourceId, context, cancellationToken)
            ?? throw new InvalidOperationException("Movie metadata not found.");
    }

    private async Task<IReadOnlyList<ExtraMetadataModel>> ResolveMovieExtrasAsync(MovieRatingKey key, MovieMetadataModel movie, PlexRequestContext context, CancellationToken cancellationToken)
    {
        if (string.Equals(key.SourceKey, _tmdbMovieSource.SourceKey, StringComparison.OrdinalIgnoreCase))
        {
            return await _tmdbMovieSource.GetExtrasAsync(key.SourceId, context, cancellationToken);
        }

        return await _tmdbMovieSource.GetExtrasByExternalIdsAsync(movie.ExternalIds, context, cancellationToken);
    }

    private MetadataResponse BuildMovieExtrasResponse(MovieMetadataModel movie, IReadOnlyList<ExtraMetadataModel> extras, PagingOptions paging)
    {
        var skip = Math.Max(0, paging.ContainerStart);
        var take = Math.Max(1, paging.ContainerSize);
        var materialized = extras
            .Skip(skip)
            .Take(take)
            .Select(extra => (object)_plexMapper.MapMovieExtra(movie, extra))
            .ToList();

        return new MetadataResponse
        {
            MediaContainer = new MediaContainer
            {
                Offset = skip,
                TotalSize = extras.Count,
                Identifier = ProviderDefinitions.MovieIdentifier,
                Size = materialized.Count,
                Metadata = materialized
            }
        };
    }

    private async Task<MetadataResponse> GetShowMetadataAsync(ShowRatingKey key, bool includeChildren, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var show = await LoadShowAsync(key, context, cancellationToken);
        return _plexMapper.WrapMetadata(ProviderDefinitions.TvIdentifier, [_plexMapper.MapShow(show, includeChildren)]);
    }

    private async Task<MetadataResponse> GetSeasonMetadataAsync(SeasonRatingKey key, bool includeChildren, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var (show, season) = await LoadSeasonContextAsync(key, context, cancellationToken);
        return _plexMapper.WrapMetadata(ProviderDefinitions.TvIdentifier, [_plexMapper.MapSeason(show, season, includeChildren)]);
    }

    private async Task<MetadataResponse> GetEpisodeMetadataAsync(EpisodeRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var (show, season, episode) = await LoadEpisodeContextAsync(key, context, cancellationToken);
        return _plexMapper.WrapMetadata(ProviderDefinitions.TvIdentifier, [_plexMapper.MapEpisode(show, season, episode)]);
    }

    private async Task<MetadataResponse> GetShowExtraMetadataAsync(ShowExtraRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var showKey = new ShowRatingKey(key.SourceKey, key.SourceId);
        var show = await LoadShowAsync(showKey, context, cancellationToken);
        var extras = await ResolveShowExtrasAsync(showKey, show, context, cancellationToken);
        var extra = extras.FirstOrDefault(item => item.Index == key.ExtraIndex)
            ?? throw new InvalidOperationException("Show extra metadata not found.");

        return _plexMapper.WrapMetadata(ProviderDefinitions.TvIdentifier, [_plexMapper.MapShowExtra(show, extra)]);
    }

    private async Task<MetadataResponse> GetSeasonExtraMetadataAsync(SeasonExtraRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var seasonKey = new SeasonRatingKey(key.SourceKey, key.SourceId, key.SeasonNumber);
        var (show, season) = await LoadSeasonContextAsync(seasonKey, context, cancellationToken);
        var extras = await ResolveSeasonExtrasAsync(seasonKey, show, context, cancellationToken);
        var extra = extras.FirstOrDefault(item => item.Index == key.ExtraIndex)
            ?? throw new InvalidOperationException("Season extra metadata not found.");

        return _plexMapper.WrapMetadata(ProviderDefinitions.TvIdentifier, [_plexMapper.MapSeasonExtra(show, season, extra)]);
    }

    private async Task<MetadataResponse> GetEpisodeExtraMetadataAsync(EpisodeExtraRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var episodeKey = new EpisodeRatingKey(key.SourceKey, key.SourceId, key.SeasonNumber, key.EpisodeNumber);
        var (show, season, episode) = await LoadEpisodeContextAsync(episodeKey, context, cancellationToken);
        var extras = await ResolveEpisodeExtrasAsync(episodeKey, show, context, cancellationToken);
        var extra = extras.FirstOrDefault(item => item.Index == key.ExtraIndex)
            ?? throw new InvalidOperationException("Episode extra metadata not found.");

        return _plexMapper.WrapMetadata(ProviderDefinitions.TvIdentifier, [_plexMapper.MapEpisodeExtra(show, season, episode, extra)]);
    }

    private async Task<MetadataResponse> GetShowImagesAsync(ShowRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var show = await LoadShowAsync(key, context, cancellationToken);
        return _plexMapper.WrapImages(ProviderDefinitions.TvIdentifier, _plexMapper.ShowImages(show) ?? []);
    }

    private async Task<MetadataResponse> GetSeasonImagesAsync(SeasonRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var (show, season) = await LoadSeasonContextAsync(key, context, cancellationToken);
        return _plexMapper.WrapImages(ProviderDefinitions.TvIdentifier, _plexMapper.SeasonImages(show, season) ?? []);
    }

    private async Task<MetadataResponse> GetEpisodeImagesAsync(EpisodeRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var (show, _, episode) = await LoadEpisodeContextAsync(key, context, cancellationToken);
        return _plexMapper.WrapImages(ProviderDefinitions.TvIdentifier, _plexMapper.EpisodeImages(show, episode) ?? []);
    }

    private async Task<MetadataResponse> GetShowExtraImagesAsync(ShowExtraRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var showKey = new ShowRatingKey(key.SourceKey, key.SourceId);
        var show = await LoadShowAsync(showKey, context, cancellationToken);
        var extras = await ResolveShowExtrasAsync(showKey, show, context, cancellationToken);
        var extra = extras.FirstOrDefault(item => item.Index == key.ExtraIndex)
            ?? throw new InvalidOperationException("Show extra images not found.");

        return _plexMapper.WrapImages(ProviderDefinitions.TvIdentifier, _plexMapper.ShowExtraImages(extra) ?? []);
    }

    private async Task<MetadataResponse> GetSeasonExtraImagesAsync(SeasonExtraRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var seasonKey = new SeasonRatingKey(key.SourceKey, key.SourceId, key.SeasonNumber);
        var (show, season) = await LoadSeasonContextAsync(seasonKey, context, cancellationToken);
        var extras = await ResolveSeasonExtrasAsync(seasonKey, show, context, cancellationToken);
        var extra = extras.FirstOrDefault(item => item.Index == key.ExtraIndex)
            ?? throw new InvalidOperationException("Season extra images not found.");

        return _plexMapper.WrapImages(ProviderDefinitions.TvIdentifier, _plexMapper.SeasonExtraImages(show, season, extra) ?? []);
    }

    private async Task<MetadataResponse> GetEpisodeExtraImagesAsync(EpisodeExtraRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var episodeKey = new EpisodeRatingKey(key.SourceKey, key.SourceId, key.SeasonNumber, key.EpisodeNumber);
        var (show, season, episode) = await LoadEpisodeContextAsync(episodeKey, context, cancellationToken);
        var extras = await ResolveEpisodeExtrasAsync(episodeKey, show, context, cancellationToken);
        var extra = extras.FirstOrDefault(item => item.Index == key.ExtraIndex)
            ?? throw new InvalidOperationException("Episode extra images not found.");

        return _plexMapper.WrapImages(ProviderDefinitions.TvIdentifier, _plexMapper.EpisodeExtraImages(show, season, episode, extra) ?? []);
    }

    private async Task<ShowMetadataModel> LoadShowAsync(ShowRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var source = _sourceRegistry.GetTvSource(key.SourceKey)
            ?? throw new InvalidOperationException($"TV source '{key.SourceKey}' is not enabled.");

        return await source.GetShowAsync(key.SourceId, context, cancellationToken)
            ?? throw new InvalidOperationException("Show metadata not found.");
    }

    private async Task<(ShowMetadataModel Show, SeasonMetadataModel Season)> LoadSeasonContextAsync(SeasonRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var source = _sourceRegistry.GetTvSource(key.SourceKey)
            ?? throw new InvalidOperationException($"TV source '{key.SourceKey}' is not enabled.");
        var show = await source.GetShowAsync(key.SourceId, context, cancellationToken)
            ?? throw new InvalidOperationException("Show metadata not found.");
        var season = await source.GetSeasonAsync(key.SourceId, key.SeasonNumber, context, cancellationToken)
            ?? throw new InvalidOperationException("Season metadata not found.");
        return (show, season);
    }

    private async Task<(ShowMetadataModel Show, SeasonMetadataModel Season, EpisodeMetadataModel Episode)> LoadEpisodeContextAsync(EpisodeRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var source = _sourceRegistry.GetTvSource(key.SourceKey)
            ?? throw new InvalidOperationException($"TV source '{key.SourceKey}' is not enabled.");
        var show = await source.GetShowAsync(key.SourceId, context, cancellationToken)
            ?? throw new InvalidOperationException("Show metadata not found.");
        var season = await source.GetSeasonAsync(key.SourceId, key.SeasonNumber, context, cancellationToken)
            ?? throw new InvalidOperationException("Season metadata not found.");
        var episode = await source.GetEpisodeAsync(key.SourceId, key.SeasonNumber, key.EpisodeNumber, context, cancellationToken)
            ?? throw new InvalidOperationException("Episode metadata not found.");
        return (show, season, episode);
    }

    private async Task<IReadOnlyList<ExtraMetadataModel>> ResolveShowExtrasAsync(ShowRatingKey key, ShowMetadataModel show, PlexRequestContext context, CancellationToken cancellationToken)
    {
        if (string.Equals(key.SourceKey, _tmdbTvSource.SourceKey, StringComparison.OrdinalIgnoreCase))
        {
            return await _tmdbTvSource.GetShowExtrasAsync(key.SourceId, context, cancellationToken);
        }

        return await _tmdbTvSource.GetShowExtrasByExternalIdsAsync(show.ExternalIds, context, cancellationToken);
    }

    private async Task<IReadOnlyList<ExtraMetadataModel>> ResolveSeasonExtrasAsync(SeasonRatingKey key, ShowMetadataModel show, PlexRequestContext context, CancellationToken cancellationToken)
    {
        if (string.Equals(key.SourceKey, _tmdbTvSource.SourceKey, StringComparison.OrdinalIgnoreCase))
        {
            return await _tmdbTvSource.GetSeasonExtrasAsync(key.SourceId, key.SeasonNumber, context, cancellationToken);
        }

        return await _tmdbTvSource.GetSeasonExtrasByExternalIdsAsync(show.ExternalIds, key.SeasonNumber, context, cancellationToken);
    }

    private async Task<IReadOnlyList<ExtraMetadataModel>> ResolveEpisodeExtrasAsync(EpisodeRatingKey key, ShowMetadataModel show, PlexRequestContext context, CancellationToken cancellationToken)
    {
        if (string.Equals(key.SourceKey, _tmdbTvSource.SourceKey, StringComparison.OrdinalIgnoreCase))
        {
            return await _tmdbTvSource.GetEpisodeExtrasAsync(key.SourceId, key.SeasonNumber, key.EpisodeNumber, context, cancellationToken);
        }

        return await _tmdbTvSource.GetEpisodeExtrasByExternalIdsAsync(show.ExternalIds, key.SeasonNumber, key.EpisodeNumber, context, cancellationToken);
    }

    private MetadataResponse BuildShowExtrasResponse(ShowMetadataModel show, IReadOnlyList<ExtraMetadataModel> extras, PagingOptions paging)
    {
        var skip = Math.Max(0, paging.ContainerStart);
        var take = Math.Max(1, paging.ContainerSize);
        var materialized = extras
            .Skip(skip)
            .Take(take)
            .Select(extra => (object)_plexMapper.MapShowExtra(show, extra))
            .ToList();

        return new MetadataResponse
        {
            MediaContainer = new MediaContainer
            {
                Offset = skip,
                TotalSize = extras.Count,
                Identifier = ProviderDefinitions.TvIdentifier,
                Size = materialized.Count,
                Metadata = materialized
            }
        };
    }

    private MetadataResponse BuildSeasonExtrasResponse(ShowMetadataModel show, SeasonMetadataModel season, IReadOnlyList<ExtraMetadataModel> extras, PagingOptions paging)
    {
        var skip = Math.Max(0, paging.ContainerStart);
        var take = Math.Max(1, paging.ContainerSize);
        var materialized = extras
            .Skip(skip)
            .Take(take)
            .Select(extra => (object)_plexMapper.MapSeasonExtra(show, season, extra))
            .ToList();

        return new MetadataResponse
        {
            MediaContainer = new MediaContainer
            {
                Offset = skip,
                TotalSize = extras.Count,
                Identifier = ProviderDefinitions.TvIdentifier,
                Size = materialized.Count,
                Metadata = materialized
            }
        };
    }

    private MetadataResponse BuildEpisodeExtrasResponse(ShowMetadataModel show, SeasonMetadataModel season, EpisodeMetadataModel episode, IReadOnlyList<ExtraMetadataModel> extras, PagingOptions paging)
    {
        var skip = Math.Max(0, paging.ContainerStart);
        var take = Math.Max(1, paging.ContainerSize);
        var materialized = extras
            .Skip(skip)
            .Take(take)
            .Select(extra => (object)_plexMapper.MapEpisodeExtra(show, season, episode, extra))
            .ToList();

        return new MetadataResponse
        {
            MediaContainer = new MediaContainer
            {
                Offset = skip,
                TotalSize = extras.Count,
                Identifier = ProviderDefinitions.TvIdentifier,
                Size = materialized.Count,
                Metadata = materialized
            }
        };
    }

    private static MetadataResponse EmptyExtrasResponse(string identifier, PagingOptions paging)
        => new()
        {
            MediaContainer = new MediaContainer
            {
                Offset = Math.Max(0, paging.ContainerStart),
                TotalSize = 0,
                Identifier = identifier,
                Size = 0,
                Metadata = []
            }
        };
}
