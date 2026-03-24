using System.Text.Json.Serialization;

namespace PlexModernMetadataProvider.Api.Models;

public sealed class TmdbSearchResponse<T>
{
    [JsonPropertyName("results")]
    public List<T> Results { get; set; } = [];
}

public sealed class TmdbFindResponse
{
    [JsonPropertyName("movie_results")]
    public List<TmdbMovieSummary> MovieResults { get; set; } = [];

    [JsonPropertyName("tv_results")]
    public List<TmdbTvSummary> TvResults { get; set; } = [];
}

public sealed class TmdbMovieSummary
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("original_title")]
    public string? OriginalTitle { get; set; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("popularity")]
    public double Popularity { get; set; }

    [JsonPropertyName("adult")]
    public bool Adult { get; set; }
}

public sealed class TmdbTvSummary
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("original_name")]
    public string? OriginalName { get; set; }

    [JsonPropertyName("first_air_date")]
    public string? FirstAirDate { get; set; }

    [JsonPropertyName("popularity")]
    public double Popularity { get; set; }

    [JsonPropertyName("adult")]
    public bool? Adult { get; set; }
}

public sealed class TmdbMovieDetails
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("original_title")]
    public string? OriginalTitle { get; set; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("poster_path")]
    public string? PosterPath { get; set; }

    [JsonPropertyName("backdrop_path")]
    public string? BackdropPath { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("adult")]
    public bool Adult { get; set; }

    [JsonPropertyName("runtime")]
    public int? Runtime { get; set; }

    [JsonPropertyName("tagline")]
    public string? Tagline { get; set; }

    [JsonPropertyName("vote_average")]
    public double VoteAverage { get; set; }

    [JsonPropertyName("genres")]
    public List<TmdbNamedEntity>? Genres { get; set; }

    [JsonPropertyName("production_countries")]
    public List<TmdbNamedEntity>? ProductionCountries { get; set; }

    [JsonPropertyName("production_companies")]
    public List<TmdbNamedEntity>? ProductionCompanies { get; set; }

    [JsonPropertyName("credits")]
    public TmdbCredits? Credits { get; set; }

    [JsonPropertyName("external_ids")]
    public TmdbExternalIds? ExternalIds { get; set; }

    [JsonPropertyName("images")]
    public TmdbImageCollection? Images { get; set; }

    [JsonPropertyName("release_dates")]
    public TmdbReleaseDates? ReleaseDates { get; set; }

    [JsonPropertyName("videos")]
    public TmdbVideoCollection? Videos { get; set; }
}

public sealed class TmdbTvDetails
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("original_name")]
    public string? OriginalName { get; set; }

    [JsonPropertyName("first_air_date")]
    public string? FirstAirDate { get; set; }

    [JsonPropertyName("poster_path")]
    public string? PosterPath { get; set; }

    [JsonPropertyName("backdrop_path")]
    public string? BackdropPath { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("adult")]
    public bool? Adult { get; set; }

    [JsonPropertyName("tagline")]
    public string? Tagline { get; set; }

    [JsonPropertyName("vote_average")]
    public double VoteAverage { get; set; }

    [JsonPropertyName("episode_run_time")]
    public List<int>? EpisodeRunTime { get; set; }

    [JsonPropertyName("genres")]
    public List<TmdbNamedEntity>? Genres { get; set; }

    [JsonPropertyName("production_countries")]
    public List<TmdbNamedEntity>? ProductionCountries { get; set; }

    [JsonPropertyName("production_companies")]
    public List<TmdbNamedEntity>? ProductionCompanies { get; set; }

    [JsonPropertyName("networks")]
    public List<TmdbNamedEntity>? Networks { get; set; }

    [JsonPropertyName("seasons")]
    public List<TmdbSeasonSummary>? Seasons { get; set; }

    [JsonPropertyName("credits")]
    public TmdbCredits? Credits { get; set; }

    [JsonPropertyName("aggregate_credits")]
    public TmdbCredits? AggregateCredits { get; set; }

    [JsonPropertyName("external_ids")]
    public TmdbExternalIds? ExternalIds { get; set; }

    [JsonPropertyName("images")]
    public TmdbImageCollection? Images { get; set; }

    [JsonPropertyName("content_ratings")]
    public TmdbContentRatings? ContentRatings { get; set; }

    [JsonPropertyName("videos")]
    public TmdbVideoCollection? Videos { get; set; }
}

public class TmdbSeasonSummary
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("season_number")]
    public int SeasonNumber { get; set; }

    [JsonPropertyName("air_date")]
    public string? AirDate { get; set; }

    [JsonPropertyName("poster_path")]
    public string? PosterPath { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("episodes")]
    public List<TmdbEpisodeSummary>? Episodes { get; set; }

    [JsonPropertyName("images")]
    public TmdbImageCollection? Images { get; set; }
}

public sealed class TmdbSeasonDetails : TmdbSeasonSummary
{
    [JsonPropertyName("credits")]
    public TmdbCredits? Credits { get; set; }

    [JsonPropertyName("external_ids")]
    public TmdbExternalIds? ExternalIds { get; set; }

    [JsonPropertyName("videos")]
    public TmdbVideoCollection? Videos { get; set; }
}

public class TmdbEpisodeSummary
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("episode_number")]
    public int EpisodeNumber { get; set; }

    [JsonPropertyName("air_date")]
    public string? AirDate { get; set; }

    [JsonPropertyName("still_path")]
    public string? StillPath { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("runtime")]
    public int? Runtime { get; set; }

    [JsonPropertyName("vote_average")]
    public double VoteAverage { get; set; }
}

public sealed class TmdbEpisodeDetails : TmdbEpisodeSummary
{
    [JsonPropertyName("credits")]
    public TmdbCredits? Credits { get; set; }

    [JsonPropertyName("images")]
    public TmdbImageCollection? Images { get; set; }

    [JsonPropertyName("external_ids")]
    public TmdbExternalIds? ExternalIds { get; set; }

    [JsonPropertyName("videos")]
    public TmdbVideoCollection? Videos { get; set; }
}

public sealed class TmdbNamedEntity
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class TmdbExternalIds
{
    [JsonPropertyName("imdb_id")]
    public string? ImdbId { get; set; }

    [JsonPropertyName("tvdb_id")]
    public int? TvdbId { get; set; }
}

public sealed class TmdbCredits
{
    [JsonPropertyName("cast")]
    public List<TmdbCastMember>? Cast { get; set; }

    [JsonPropertyName("crew")]
    public List<TmdbCrewMember>? Crew { get; set; }
}

public sealed class TmdbCastMember
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("character")]
    public string? Character { get; set; }

    [JsonPropertyName("roles")]
    public List<TmdbCastRole>? Roles { get; set; }

    [JsonPropertyName("profile_path")]
    public string? ProfilePath { get; set; }

    [JsonPropertyName("order")]
    public int? Order { get; set; }
}

public sealed class TmdbCastRole
{
    [JsonPropertyName("character")]
    public string? Character { get; set; }
}

public sealed class TmdbCrewMember
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("profile_path")]
    public string? ProfilePath { get; set; }

    [JsonPropertyName("job")]
    public string? Job { get; set; }

    [JsonPropertyName("jobs")]
    public List<TmdbCrewJob>? Jobs { get; set; }
}

public sealed class TmdbCrewJob
{
    [JsonPropertyName("job")]
    public string? Job { get; set; }
}

public sealed class TmdbImageCollection
{
    [JsonPropertyName("posters")]
    public List<TmdbImageFile>? Posters { get; set; }

    [JsonPropertyName("backdrops")]
    public List<TmdbImageFile>? Backdrops { get; set; }

    [JsonPropertyName("logos")]
    public List<TmdbImageFile>? Logos { get; set; }

    [JsonPropertyName("stills")]
    public List<TmdbImageFile>? Stills { get; set; }
}

public sealed class TmdbImageFile
{
    [JsonPropertyName("file_path")]
    public string? FilePath { get; set; }
}

public sealed class TmdbReleaseDates
{
    [JsonPropertyName("results")]
    public List<TmdbReleaseDateCountry>? Results { get; set; }
}

public sealed class TmdbReleaseDateCountry
{
    [JsonPropertyName("iso_3166_1")]
    public string? Iso31661 { get; set; }

    [JsonPropertyName("release_dates")]
    public List<TmdbReleaseDateEntry>? ReleaseDates { get; set; }
}

public sealed class TmdbReleaseDateEntry
{
    [JsonPropertyName("certification")]
    public string? Certification { get; set; }
}

public sealed class TmdbContentRatings
{
    [JsonPropertyName("results")]
    public List<TmdbContentRating>? Results { get; set; }
}

public sealed class TmdbContentRating
{
    [JsonPropertyName("iso_3166_1")]
    public string? Iso31661 { get; set; }

    [JsonPropertyName("rating")]
    public string? Rating { get; set; }
}

public sealed class TmdbVideoCollection
{
    [JsonPropertyName("results")]
    public List<TmdbVideoResult>? Results { get; set; }
}

public sealed class TmdbVideoResult
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("iso_639_1")]
    public string? Iso6391 { get; set; }

    [JsonPropertyName("iso_3166_1")]
    public string? Iso31661 { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("official")]
    public bool Official { get; set; }

    [JsonPropertyName("published_at")]
    public string? PublishedAt { get; set; }

    [JsonPropertyName("site")]
    public string? Site { get; set; }

    [JsonPropertyName("size")]
    public int? Size { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
