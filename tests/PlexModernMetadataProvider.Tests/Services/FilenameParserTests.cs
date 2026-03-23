using PlexModernMetadataProvider.Api.Services;

namespace PlexModernMetadataProvider.Tests.Services;

[TestClass]
public sealed class FilenameParserTests
{
    private readonly FilenameParser _parser = new();

    [TestMethod]
    public void Parse_ExtractsStandaloneYearFromMovieFilename()
    {
        var result = _parser.Parse("Rooster.2013.1080p.WEB-DL.mkv");

        Assert.AreEqual("Rooster", result.Title);
        Assert.AreEqual(2013, result.Year);
        Assert.IsNull(result.AirDate);
    }

    [TestMethod]
    public void Parse_ExtractsSeasonAndEpisode()
    {
        var result = _parser.Parse("Rooster.S01E02.1080p.WEB-DL.mkv");

        Assert.AreEqual("Rooster", result.Title);
        Assert.AreEqual(1, result.Season);
        Assert.AreEqual(2, result.Episode);
    }

    [TestMethod]
    public void Parse_UsesAirDateWithoutTreatingItAsStandaloneYear()
    {
        var result = _parser.Parse("Daily.Report.2026-03-23.1080p.WEB-DL.mkv");

        Assert.AreEqual("Daily Report", result.Title);
        Assert.AreEqual("2026-03-23", result.AirDate);
        Assert.IsNull(result.Year);
    }
}
