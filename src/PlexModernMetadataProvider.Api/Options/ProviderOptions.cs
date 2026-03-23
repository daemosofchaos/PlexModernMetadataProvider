namespace PlexModernMetadataProvider.Api.Options;

public sealed class ProviderOptions
{
    public const string SectionName = "Provider";

    public string DefaultLanguage { get; set; } = "en-US";
    public string DefaultCountry { get; set; } = "US";
    public int MaxManualMatches { get; set; } = 10;
    public string MovieSourceOrder { get; set; } = "Omdb,Tmdb";
    public string TvSourceOrder { get; set; } = "TvMaze,Tmdb";
    public TmdbOptions TMDb { get; set; } = new();
    public OmdbOptions OMDb { get; set; } = new();
    public TvMazeOptions TVMaze { get; set; } = new();
}

public sealed class TmdbOptions
{
    public string BaseUrl { get; set; } = "https://api.themoviedb.org/3/";
    public string ApiKey { get; set; } = string.Empty;
    public string ReadAccessToken { get; set; } = string.Empty;
    public int RequestTimeoutSeconds { get; set; } = 15;
    public int CacheTtlMinutes { get; set; } = 15;
}

public sealed class OmdbOptions
{
    public string BaseUrl { get; set; } = "https://www.omdbapi.com/";
    public string ApiKey { get; set; } = string.Empty;
    public int RequestTimeoutSeconds { get; set; } = 15;
    public int CacheTtlMinutes { get; set; } = 15;
}

public sealed class TvMazeOptions
{
    public string BaseUrl { get; set; } = "https://api.tvmaze.com/";
    public int RequestTimeoutSeconds { get; set; } = 15;
    public int CacheTtlMinutes { get; set; } = 15;
}
