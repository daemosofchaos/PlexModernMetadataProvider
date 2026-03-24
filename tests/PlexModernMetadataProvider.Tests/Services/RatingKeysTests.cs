using PlexModernMetadataProvider.Api.Services;

namespace PlexModernMetadataProvider.Tests.Services;

[TestClass]
public sealed class RatingKeysTests
{
    [TestMethod]
    public void Parse_RoundTripsMovieKey()
    {
        var key = RatingKeys.BuildMovie("omdb", "tt0123456");
        var parsed = RatingKeys.Parse(key);

        Assert.IsInstanceOfType<MovieRatingKey>(parsed);
        Assert.AreEqual("omdb", ((MovieRatingKey)parsed).SourceKey);
        Assert.AreEqual("tt0123456", ((MovieRatingKey)parsed).SourceId);
    }

    [TestMethod]
    public void Parse_RoundTripsMovieExtraKey()
    {
        var key = RatingKeys.BuildMovieExtra("omdb", "tt0123456", 2);
        var parsed = RatingKeys.Parse(key);

        Assert.IsInstanceOfType<MovieExtraRatingKey>(parsed);
        Assert.AreEqual("omdb", ((MovieExtraRatingKey)parsed).SourceKey);
        Assert.AreEqual("tt0123456", ((MovieExtraRatingKey)parsed).SourceId);
        Assert.AreEqual(2, ((MovieExtraRatingKey)parsed).ExtraIndex);
    }

    [TestMethod]
    public void Parse_RoundTripsShowKey()
    {
        var key = RatingKeys.BuildShow("tvmaze", "82");
        var parsed = RatingKeys.Parse(key);

        Assert.IsInstanceOfType<ShowRatingKey>(parsed);
        Assert.AreEqual("tvmaze", ((ShowRatingKey)parsed).SourceKey);
        Assert.AreEqual("82", ((ShowRatingKey)parsed).SourceId);
    }

    [TestMethod]
    public void Parse_RoundTripsShowExtraKey()
    {
        var key = RatingKeys.BuildShowExtra("tvmaze", "82", 3);
        var parsed = RatingKeys.Parse(key);

        Assert.IsInstanceOfType<ShowExtraRatingKey>(parsed);
        Assert.AreEqual("tvmaze", ((ShowExtraRatingKey)parsed).SourceKey);
        Assert.AreEqual("82", ((ShowExtraRatingKey)parsed).SourceId);
        Assert.AreEqual(3, ((ShowExtraRatingKey)parsed).ExtraIndex);
    }

    [TestMethod]
    public void Parse_RoundTripsSeasonKey()
    {
        var key = RatingKeys.BuildSeason("tmdb", "7", 3);
        var parsed = RatingKeys.Parse(key);

        Assert.IsInstanceOfType<SeasonRatingKey>(parsed);
        Assert.AreEqual("tmdb", ((SeasonRatingKey)parsed).SourceKey);
        Assert.AreEqual("7", ((SeasonRatingKey)parsed).SourceId);
        Assert.AreEqual(3, ((SeasonRatingKey)parsed).SeasonNumber);
    }

    [TestMethod]
    public void Parse_RoundTripsSeasonExtraKey()
    {
        var key = RatingKeys.BuildSeasonExtra("tmdb", "7", 3, 4);
        var parsed = RatingKeys.Parse(key);

        Assert.IsInstanceOfType<SeasonExtraRatingKey>(parsed);
        Assert.AreEqual("tmdb", ((SeasonExtraRatingKey)parsed).SourceKey);
        Assert.AreEqual("7", ((SeasonExtraRatingKey)parsed).SourceId);
        Assert.AreEqual(3, ((SeasonExtraRatingKey)parsed).SeasonNumber);
        Assert.AreEqual(4, ((SeasonExtraRatingKey)parsed).ExtraIndex);
    }

    [TestMethod]
    public void Parse_RoundTripsEpisodeKey()
    {
        var key = RatingKeys.BuildEpisode("tmdb", "7", 3, 5);
        var parsed = RatingKeys.Parse(key);

        Assert.IsInstanceOfType<EpisodeRatingKey>(parsed);
        Assert.AreEqual("tmdb", ((EpisodeRatingKey)parsed).SourceKey);
        Assert.AreEqual("7", ((EpisodeRatingKey)parsed).SourceId);
        Assert.AreEqual(3, ((EpisodeRatingKey)parsed).SeasonNumber);
        Assert.AreEqual(5, ((EpisodeRatingKey)parsed).EpisodeNumber);
    }

    [TestMethod]
    public void Parse_RoundTripsEpisodeExtraKey()
    {
        var key = RatingKeys.BuildEpisodeExtra("tmdb", "7", 3, 5, 6);
        var parsed = RatingKeys.Parse(key);

        Assert.IsInstanceOfType<EpisodeExtraRatingKey>(parsed);
        Assert.AreEqual("tmdb", ((EpisodeExtraRatingKey)parsed).SourceKey);
        Assert.AreEqual("7", ((EpisodeExtraRatingKey)parsed).SourceId);
        Assert.AreEqual(3, ((EpisodeExtraRatingKey)parsed).SeasonNumber);
        Assert.AreEqual(5, ((EpisodeExtraRatingKey)parsed).EpisodeNumber);
        Assert.AreEqual(6, ((EpisodeExtraRatingKey)parsed).ExtraIndex);
    }
}
