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

    [TestMethod]
    public void Parse_Handles_NoisyMovieReleaseNames()
    {
        var result = _parser.Parse("Avatar Fire and Ash 2025 2160p WEB-DL DDP5.1 Atmos SDR H265-AOC.mkv");

        Assert.AreEqual("Avatar Fire and Ash", result.Title);
        Assert.AreEqual(2025, result.Year);
        Assert.IsNull(result.Season);
        Assert.IsNull(result.Episode);
    }

    [TestMethod]
    public void Parse_Strips_LeadingSitePrefixes_FromEpisodeNames()
    {
        var result = _parser.Parse("www.UIndex.org    -    Deadliest.Catch.S21E15.1080p.HEVC.x265-MeGusta.mkv");

        Assert.AreEqual("Deadliest Catch", result.Title);
        Assert.AreEqual(21, result.Season);
        Assert.AreEqual(15, result.Episode);
    }

    [TestMethod]
    public void Parse_Prefers_ShowFolder_When_FileName_IsPolluted()
    {
        var result = _parser.Parse(@"D:\TV Shows\Deadliest Catch\Season 21\www.UIndex.org    -    Deadliest.Catch.S21E15.1080p.HEVC.x265-MeGusta.mkv");

        Assert.AreEqual("Deadliest Catch", result.Title);
        Assert.AreEqual(21, result.Season);
        Assert.AreEqual(15, result.Episode);
    }

    [TestMethod]
    public void Parse_Uses_CleanMovieFolderName_When_Available()
    {
        var result = _parser.Parse(@"D:\Movies\Avatar Fire and Ash (2025)\Avatar Fire and Ash 2025 2160p WEB-DL DDP5.1 Atmos SDR H265-AOC.mkv");

        Assert.AreEqual("Avatar Fire and Ash", result.Title);
        Assert.AreEqual(2025, result.Year);
    }
}
