using PlexModernMetadataProvider.Api.Services;

namespace PlexModernMetadataProvider.Tests.Services;

[TestClass]
public sealed class ProviderDefinitionsTests
{
    [TestMethod]
    public void Route_Composition_Produces_PlexExpected_MovieMatchPath()
    {
        var actual = $"{ProviderDefinitions.MovieBasePath}{ProviderDefinitions.MatchPath}";
        Assert.AreEqual("/movie/library/metadata/matches", actual);
    }

    [TestMethod]
    public void Route_Composition_Produces_PlexExpected_TvMatchPath()
    {
        var actual = $"{ProviderDefinitions.TvBasePath}{ProviderDefinitions.MatchPath}";
        Assert.AreEqual("/tv/library/metadata/matches", actual);
    }

    [TestMethod]
    public void Route_Composition_Produces_PlexExpected_MovieExtrasPath()
    {
        var actual = $"{ProviderDefinitions.MovieBasePath}{ProviderDefinitions.MetadataPath}/movie-tmdb-123{ProviderDefinitions.ExtrasPath}";
        Assert.AreEqual("/movie/library/metadata/movie-tmdb-123/extras", actual);
    }

    [TestMethod]
    public void Route_Composition_Produces_PlexExpected_TvExtrasPath()
    {
        var actual = $"{ProviderDefinitions.TvBasePath}{ProviderDefinitions.MetadataPath}/show-tvmaze-123{ProviderDefinitions.ExtrasPath}";
        Assert.AreEqual("/tv/library/metadata/show-tvmaze-123/extras", actual);
    }
}
