using PlexModernMetadataProvider.Api.Models;

namespace PlexModernMetadataProvider.Api.Services;

public sealed class MetadataService
{
    private readonly MetadataSourceRegistry _sourceRegistry;
    private readonly PlexMapper _plexMapper;

    public MetadataService(MetadataSourceRegistry sourceRegistry, PlexMapper plexMapper)
    {
        _sourceRegistry = sourceRegistry;
        _plexMapper = plexMapper;
    }

    public async Task<MetadataResponse> GetMovieMetadataAsync(string ratingKey, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        var parsed = RatingKeys.Parse(ratingKey);
        if (parsed is not MovieRatingKey movieKey)
        {
            throw new InvalidOperationException("Movie metadata endpoint can only serve movie keys.");
        }

        var source = _sourceRegistry.GetMovieSource(movieKey.SourceKey)
            ?? throw new InvalidOperationException($"Movie source '{movieKey.SourceKey}' is not enabled.");

        var movie = await source.GetByIdAsync(movieKey.SourceId, context, cancellationToken)
            ?? throw new InvalidOperationException("Movie metadata not found.");

        return _plexMapper.WrapMetadata(ProviderDefinitions.MovieIdentifier, [_plexMapper.MapMovie(movie)]);
    }

    public async Task<MetadataResponse> GetMovieImagesAsync(string ratingKey, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        var parsed = RatingKeys.Parse(ratingKey);
        if (parsed is not MovieRatingKey movieKey)
        {
            throw new InvalidOperationException("Movie image endpoint can only serve movie keys.");
        }

        var source = _sourceRegistry.GetMovieSource(movieKey.SourceKey)
            ?? throw new InvalidOperationException($"Movie source '{movieKey.SourceKey}' is not enabled.");

        var movie = await source.GetByIdAsync(movieKey.SourceId, context, cancellationToken)
            ?? throw new InvalidOperationException("Movie images not found.");

        return _plexMapper.WrapImages(ProviderDefinitions.MovieIdentifier, _plexMapper.MovieImages(movie) ?? []);
    }

    public MetadataResponse GetMovieExtras(PagingOptions paging)
        => EmptyExtrasResponse(ProviderDefinitions.MovieIdentifier, paging);

    public async Task<MetadataResponse> GetTvMetadataAsync(string ratingKey, PlexRequestContext context, bool includeChildren, CancellationToken cancellationToken = default)
    {
        return RatingKeys.Parse(ratingKey) switch
        {
            ShowRatingKey showKey => await GetShowMetadataAsync(showKey, includeChildren, context, cancellationToken),
            SeasonRatingKey seasonKey => await GetSeasonMetadataAsync(seasonKey, includeChildren, context, cancellationToken),
            EpisodeRatingKey episodeKey => await GetEpisodeMetadataAsync(episodeKey, context, cancellationToken),
            _ => throw new InvalidOperationException("TV metadata endpoint can only serve show, season, and episode keys.")
        };
    }

    public async Task<MetadataResponse> GetTvImagesAsync(string ratingKey, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        return RatingKeys.Parse(ratingKey) switch
        {
            ShowRatingKey showKey => await GetShowImagesAsync(showKey, context, cancellationToken),
            SeasonRatingKey seasonKey => await GetSeasonImagesAsync(seasonKey, context, cancellationToken),
            EpisodeRatingKey episodeKey => await GetEpisodeImagesAsync(episodeKey, context, cancellationToken),
            _ => throw new InvalidOperationException("TV image endpoint can only serve show, season, and episode keys.")
        };
    }

    public MetadataResponse GetTvExtras(PagingOptions paging)
        => EmptyExtrasResponse(ProviderDefinitions.TvIdentifier, paging);

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

    private async Task<MetadataResponse> GetShowMetadataAsync(ShowRatingKey key, bool includeChildren, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var source = _sourceRegistry.GetTvSource(key.SourceKey)
            ?? throw new InvalidOperationException($"TV source '{key.SourceKey}' is not enabled.");
        var show = await source.GetShowAsync(key.SourceId, context, cancellationToken)
            ?? throw new InvalidOperationException("Show metadata not found.");
        return _plexMapper.WrapMetadata(ProviderDefinitions.TvIdentifier, [_plexMapper.MapShow(show, includeChildren)]);
    }

    private async Task<MetadataResponse> GetSeasonMetadataAsync(SeasonRatingKey key, bool includeChildren, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var source = _sourceRegistry.GetTvSource(key.SourceKey)
            ?? throw new InvalidOperationException($"TV source '{key.SourceKey}' is not enabled.");
        var show = await source.GetShowAsync(key.SourceId, context, cancellationToken)
            ?? throw new InvalidOperationException("Show metadata not found.");
        var season = await source.GetSeasonAsync(key.SourceId, key.SeasonNumber, context, cancellationToken)
            ?? throw new InvalidOperationException("Season metadata not found.");
        return _plexMapper.WrapMetadata(ProviderDefinitions.TvIdentifier, [_plexMapper.MapSeason(show, season, includeChildren)]);
    }

    private async Task<MetadataResponse> GetEpisodeMetadataAsync(EpisodeRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var source = _sourceRegistry.GetTvSource(key.SourceKey)
            ?? throw new InvalidOperationException($"TV source '{key.SourceKey}' is not enabled.");
        var show = await source.GetShowAsync(key.SourceId, context, cancellationToken)
            ?? throw new InvalidOperationException("Show metadata not found.");
        var season = await source.GetSeasonAsync(key.SourceId, key.SeasonNumber, context, cancellationToken)
            ?? throw new InvalidOperationException("Season metadata not found.");
        var episode = await source.GetEpisodeAsync(key.SourceId, key.SeasonNumber, key.EpisodeNumber, context, cancellationToken)
            ?? throw new InvalidOperationException("Episode metadata not found.");
        return _plexMapper.WrapMetadata(ProviderDefinitions.TvIdentifier, [_plexMapper.MapEpisode(show, season, episode)]);
    }

    private async Task<MetadataResponse> GetShowImagesAsync(ShowRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var source = _sourceRegistry.GetTvSource(key.SourceKey)
            ?? throw new InvalidOperationException($"TV source '{key.SourceKey}' is not enabled.");
        var show = await source.GetShowAsync(key.SourceId, context, cancellationToken)
            ?? throw new InvalidOperationException("Show images not found.");
        return _plexMapper.WrapImages(ProviderDefinitions.TvIdentifier, _plexMapper.ShowImages(show) ?? []);
    }

    private async Task<MetadataResponse> GetSeasonImagesAsync(SeasonRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var source = _sourceRegistry.GetTvSource(key.SourceKey)
            ?? throw new InvalidOperationException($"TV source '{key.SourceKey}' is not enabled.");
        var show = await source.GetShowAsync(key.SourceId, context, cancellationToken)
            ?? throw new InvalidOperationException("Show images not found.");
        var season = await source.GetSeasonAsync(key.SourceId, key.SeasonNumber, context, cancellationToken)
            ?? throw new InvalidOperationException("Season images not found.");
        return _plexMapper.WrapImages(ProviderDefinitions.TvIdentifier, _plexMapper.SeasonImages(show, season) ?? []);
    }

    private async Task<MetadataResponse> GetEpisodeImagesAsync(EpisodeRatingKey key, PlexRequestContext context, CancellationToken cancellationToken)
    {
        var source = _sourceRegistry.GetTvSource(key.SourceKey)
            ?? throw new InvalidOperationException($"TV source '{key.SourceKey}' is not enabled.");
        var show = await source.GetShowAsync(key.SourceId, context, cancellationToken)
            ?? throw new InvalidOperationException("Show images not found.");
        var episode = await source.GetEpisodeAsync(key.SourceId, key.SeasonNumber, key.EpisodeNumber, context, cancellationToken)
            ?? throw new InvalidOperationException("Episode images not found.");
        return _plexMapper.WrapImages(ProviderDefinitions.TvIdentifier, _plexMapper.EpisodeImages(show, episode) ?? []);
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
