using PlexModernMetadataProvider.Api.Models;
using PlexModernMetadataProvider.Api.Services;

namespace PlexModernMetadataProvider.Tests.Services;

[TestClass]
public sealed class PlexExtrasTests
{
    private readonly PlexMapper _mapper = new();

    [TestMethod]
    public void MapMovie_Includes_PrimaryExtraKey_When_Provided()
    {
        var movie = new MovieMetadataModel
        {
            SourceKey = "omdb",
            SourceId = "tt0123456",
            Title = "Example Movie",
            ReleaseDate = new DateOnly(2026, 1, 1)
        };

        var item = _mapper.MapMovie(movie, "/movie/library/metadata/clip-movie-omdb-tt0123456-1");

        Assert.AreEqual("/movie/library/metadata/clip-movie-omdb-tt0123456-1", item.PrimaryExtraKey);
    }

    [TestMethod]
    public void MapMovieExtra_Produces_Clip_Metadata()
    {
        var movie = new MovieMetadataModel
        {
            SourceKey = "tmdb",
            SourceId = "1198994",
            Title = "Send Help",
            ReleaseDate = new DateOnly(2026, 1, 28),
            ThumbUrl = "https://example.com/poster.jpg",
            ArtUrl = "https://example.com/art.jpg"
        };

        var extra = new ExtraMetadataModel
        {
            SourceKey = "tmdb",
            SourceId = "abc123",
            Title = "Official Trailer",
            Subtype = "trailer",
            ThumbUrl = "https://example.com/trailer-thumb.jpg",
            ArtUrl = "https://example.com/trailer-art.jpg",
            OriginallyAvailableAt = new DateOnly(2025, 12, 1),
            Index = 1,
            IsPrimary = true
        };

        var item = _mapper.MapMovieExtra(movie, extra);

        Assert.AreEqual("clip", item.Type);
        Assert.AreEqual("trailer", item.Subtype);
        Assert.AreEqual("clip-movie-tmdb-1198994-1", item.RatingKey);
        Assert.AreEqual("/movie/library/metadata/clip-movie-tmdb-1198994-1", item.Key);
        Assert.AreEqual("Official Trailer", item.Title);
    }

    [TestMethod]
    public void MapShowExtra_Produces_Clip_Metadata()
    {
        var show = new ShowMetadataModel
        {
            SourceKey = "tvmaze",
            SourceId = "82",
            Title = "Game of Thrones",
            FirstAirDate = new DateOnly(2011, 4, 17),
            ThumbUrl = "https://example.com/show-poster.jpg",
            ArtUrl = "https://example.com/show-art.jpg"
        };

        var extra = new ExtraMetadataModel
        {
            SourceKey = "tmdb",
            SourceId = "trailer01",
            Title = "Season Preview",
            Subtype = "featurette",
            ThumbUrl = "https://example.com/show-extra-thumb.jpg",
            ArtUrl = "https://example.com/show-extra-art.jpg",
            OriginallyAvailableAt = new DateOnly(2011, 3, 1),
            Index = 2,
            IsPrimary = false
        };

        var item = _mapper.MapShowExtra(show, extra);

        Assert.AreEqual("clip", item.Type);
        Assert.AreEqual("featurette", item.Subtype);
        Assert.AreEqual("clip-show-tvmaze-82-2", item.RatingKey);
        Assert.AreEqual("/tv/library/metadata/clip-show-tvmaze-82-2", item.Key);
    }
}
