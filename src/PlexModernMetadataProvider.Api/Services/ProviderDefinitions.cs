using PlexModernMetadataProvider.Api.Models;

namespace PlexModernMetadataProvider.Api.Services;

public static class ProviderDefinitions
{
    public const string MovieBasePath = "/movie";
    public const string TvBasePath = "/tv";
    public const string MetadataPath = @"/library/metadata";
    public const string MatchPath = @"/library/metadata/matches";

    public const string MovieIdentifier = "tv.plex.agents.custom.modernmetadata.movie";
    public const string TvIdentifier = "tv.plex.agents.custom.modernmetadata.tv";
    public const string Version = "1.0.0";

    public static MediaProviderResponse CreateMovieProvider() => new()
    {
        MediaProvider = new MediaProviderDefinition
        {
            Identifier = MovieIdentifier,
            Title = "Modern Metadata Movie Provider (.NET)",
            Version = Version,
            Types =
            [
                new ProviderTypeDefinition
                {
                    Type = 1,
                    Scheme = [new ProviderScheme { Scheme = MovieIdentifier }]
                }
            ],
            Feature =
            [
                new ProviderFeature { Type = "metadata", Key = MetadataPath },
                new ProviderFeature { Type = "match", Key = MatchPath }
            ]
        }
    };

    public static MediaProviderResponse CreateTvProvider() => new()
    {
        MediaProvider = new MediaProviderDefinition
        {
            Identifier = TvIdentifier,
            Title = "Modern Metadata TV Provider (.NET)",
            Version = Version,
            Types =
            [
                new ProviderTypeDefinition
                {
                    Type = 2,
                    Scheme = [new ProviderScheme { Scheme = TvIdentifier }]
                },
                new ProviderTypeDefinition
                {
                    Type = 3,
                    Scheme = [new ProviderScheme { Scheme = TvIdentifier }]
                },
                new ProviderTypeDefinition
                {
                    Type = 4,
                    Scheme = [new ProviderScheme { Scheme = TvIdentifier }]
                }
            ],
            Feature =
            [
                new ProviderFeature { Type = "metadata", Key = MetadataPath },
                new ProviderFeature { Type = "match", Key = MatchPath }
            ]
        }
    };
}


