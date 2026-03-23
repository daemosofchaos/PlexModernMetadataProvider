using PlexModernMetadataProvider.Api.Models;
using PlexModernMetadataProvider.Api.Options;

namespace PlexModernMetadataProvider.Api.Services;

public sealed class TmdbMovieSource : IMovieMetadataSource
{
    private const string ImageBase = "https://image.tmdb.org/t/p";
    private readonly ITmdbClient _client;
    private readonly TmdbOptions _options;

    public TmdbMovieSource(ITmdbClient client, Microsoft.Extensions.Options.IOptions<ProviderOptions> options)
    {
        _client = client;
        _options = options.Value.TMDb;
    }

    public string SourceKey => "tmdb";
    public bool IsEnabled => !string.IsNullOrWhiteSpace(_options.ReadAccessToken) || !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<MovieMetadataModel?> FindByExternalGuidAsync(ExternalGuid guid, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return null;
        }

        if (guid.Provider == "tmdb" && int.TryParse(guid.Id, out var tmdbId))
        {
            return await GetByIdAsync(tmdbId.ToString(), context, cancellationToken);
        }

        if (guid.Provider != "imdb")
        {
            return null;
        }

        var find = await _client.FindByExternalIdAsync(guid.Id, "imdb_id", context.Language, cancellationToken);
        var movie = find.MovieResults.FirstOrDefault();
        return movie is null ? null : await GetByIdAsync(movie.Id.ToString(), context, cancellationToken);
    }

    public async Task<IReadOnlyList<MovieSearchCandidate>> SearchAsync(string queryTitle, int? requestedYear, bool includeAdult, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(queryTitle))
        {
            return [];
        }

        var search = await _client.SearchMoviesAsync(queryTitle, context.Language, requestedYear, includeAdult, cancellationToken);
        return search.Results
            .Where(item => includeAdult || !item.Adult)
            .Select(item => new MovieSearchCandidate
            {
                SourceKey = SourceKey,
                SourceId = item.Id.ToString(),
                Title = item.Title,
                OriginalTitle = item.OriginalTitle,
                ReleaseDate = ParseDate(item.ReleaseDate),
                Popularity = item.Popularity,
                IsAdult = item.Adult
            })
            .ToList();
    }

    public async Task<MovieMetadataModel?> GetByIdAsync(string sourceId, PlexRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || !int.TryParse(sourceId, out var tmdbId))
        {
            return null;
        }

        var movie = await _client.GetMovieAsync(tmdbId, context.Language, cancellationToken);
        return new MovieMetadataModel
        {
            SourceKey = SourceKey,
            SourceId = movie.Id.ToString(),
            Title = movie.Title ?? $"Movie {movie.Id}",
            OriginalTitle = DifferentOrNull(movie.OriginalTitle, movie.Title),
            ReleaseDate = ParseDate(movie.ReleaseDate),
            Summary = NullIfWhiteSpace(movie.Overview),
            IsAdult = movie.Adult ? true : null,
            RuntimeMinutes = movie.Runtime is > 0 ? movie.Runtime : null,
            Tagline = NullIfWhiteSpace(movie.Tagline),
            Studio = FirstName(movie.ProductionCompanies),
            Rating = movie.VoteAverage > 0 ? Math.Round(movie.VoteAverage, 1) : null,
            RatingImage = "themoviedb://image.rating",
            ContentRating = MovieContentRating(movie, context.Country),
            ThumbUrl = ImageUrl(movie.PosterPath),
            ArtUrl = ImageUrl(movie.BackdropPath),
            Images = BuildImages(movie),
            Genres = Names(movie.Genres),
            Countries = Names(movie.ProductionCountries),
            Studios = Names(movie.ProductionCompanies),
            ExternalIds = BuildExternalIds(movie.Id, movie.ExternalIds?.ImdbId, null),
            Cast = Cast(movie.Credits?.Cast),
            Directors = Crew(movie.Credits?.Crew, "Director"),
            Producers = Crew(movie.Credits?.Crew, "Producer", "Executive Producer"),
            Writers = Crew(movie.Credits?.Crew, "Writer", "Screenplay", "Story")
        };
    }

    private static IReadOnlyList<SourceImage> BuildImages(TmdbMovieDetails movie)
        => DistinctImages(
        [
            .. ImageGroup(movie.Images?.Posters, "coverPoster", movie.Title),
            .. ImageGroup(movie.Images?.Backdrops, "background", movie.Title),
            .. ImageGroup(movie.Images?.Logos, "clearLogo", movie.Title),
            .. FallbackImage(ImageUrl(movie.PosterPath), "coverPoster", movie.Title),
            .. FallbackImage(ImageUrl(movie.BackdropPath), "background", movie.Title)
        ]);

    private static IReadOnlyList<ExternalIdValue> BuildExternalIds(int tmdbId, string? imdbId, int? tvdbId)
    {
        var items = new List<ExternalIdValue>
        {
            new() { Provider = "tmdb", Id = tmdbId.ToString() }
        };

        if (!string.IsNullOrWhiteSpace(imdbId))
        {
            items.Add(new ExternalIdValue { Provider = "imdb", Id = imdbId });
        }

        if (tvdbId.HasValue)
        {
            items.Add(new ExternalIdValue { Provider = "tvdb", Id = tvdbId.Value.ToString() });
        }

        return items;
    }

    private static IReadOnlyList<PersonCredit>? Cast(IEnumerable<TmdbCastMember>? items)
    {
        var cast = items?
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Take(20)
            .Select(item => new PersonCredit
            {
                Name = item.Name!,
                Role = NullIfWhiteSpace(item.Character ?? item.Roles?.FirstOrDefault()?.Character),
                Thumb = ImageUrl(item.ProfilePath, "w500"),
                Order = item.Order
            })
            .ToList();

        return cast is { Count: > 0 } ? cast : null;
    }

    private static IReadOnlyList<PersonCredit>? Crew(IEnumerable<TmdbCrewMember>? items, params string[] jobs)
    {
        var allowedJobs = jobs.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var crew = items?
            .Where(item => item.Name is not null && CrewMatches(item, allowedJobs))
            .Take(15)
            .Select(item => new PersonCredit
            {
                Name = item.Name!,
                Thumb = ImageUrl(item.ProfilePath, "w500")
            })
            .ToList();

        return crew is { Count: > 0 } ? crew : null;
    }

    private static bool CrewMatches(TmdbCrewMember item, HashSet<string> allowedJobs)
    {
        if (!string.IsNullOrWhiteSpace(item.Job) && allowedJobs.Contains(item.Job))
        {
            return true;
        }

        return item.Jobs?.Any(job => !string.IsNullOrWhiteSpace(job.Job) && allowedJobs.Contains(job.Job!)) == true;
    }

    private static IReadOnlyList<string>? Names(IEnumerable<TmdbNamedEntity>? items)
    {
        var values = items?
            .Select(item => NullIfWhiteSpace(item.Name))
            .Where(item => item is not null)
            .Select(item => item!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return values is { Count: > 0 } ? values : null;
    }

    private static string? FirstName(IEnumerable<TmdbNamedEntity>? items)
        => Names(items)?.FirstOrDefault();

    private static string? MovieContentRating(TmdbMovieDetails movie, string country)
    {
        var rating = movie.ReleaseDates?.Results?
            .FirstOrDefault(item => string.Equals(item.Iso31661, country, StringComparison.OrdinalIgnoreCase))?
            .ReleaseDates?
            .FirstOrDefault(entry => !string.IsNullOrWhiteSpace(entry.Certification))?
            .Certification;

        rating ??= movie.ReleaseDates?.Results?
            .FirstOrDefault(item => string.Equals(item.Iso31661, "US", StringComparison.OrdinalIgnoreCase))?
            .ReleaseDates?
            .FirstOrDefault(entry => !string.IsNullOrWhiteSpace(entry.Certification))?
            .Certification;

        if (string.IsNullOrWhiteSpace(rating))
        {
            return null;
        }

        return string.Equals(country, "US", StringComparison.OrdinalIgnoreCase)
            ? rating
            : $"{country.ToLowerInvariant()}/{rating}";
    }

    private static IReadOnlyList<SourceImage> ImageGroup(IEnumerable<TmdbImageFile>? items, string type, string? alt)
        => items?
            .Select(item => ImageUrl(item.FilePath))
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => new SourceImage { Type = type, Url = url!, Alt = alt })
            .ToList()
           ?? [];

    private static IReadOnlyList<SourceImage> FallbackImage(string? url, string type, string? alt)
        => string.IsNullOrWhiteSpace(url)
            ? []
            : [new SourceImage { Type = type, Url = url, Alt = alt }];

    private static IReadOnlyList<SourceImage> DistinctImages(IEnumerable<SourceImage> images)
        => images.GroupBy(image => image.Url, StringComparer.OrdinalIgnoreCase).Select(group => group.First()).ToList();

    private static string? ImageUrl(string? path, string size = "original")
        => string.IsNullOrWhiteSpace(path) ? null : $"{ImageBase}/{size}{path}";

    private static DateOnly? ParseDate(string? value)
        => DateOnly.TryParse(value, out var parsed) ? parsed : null;

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? DifferentOrNull(string? value, string? current)
        => string.Equals(value, current, StringComparison.OrdinalIgnoreCase) ? null : NullIfWhiteSpace(value);
}
