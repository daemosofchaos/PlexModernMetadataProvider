using PlexModernMetadataProvider.Api.Services;

namespace PlexModernMetadataProvider.Tests.Services;

[TestClass]
public sealed class ProviderDefinitionsTests
{
    [TestMethod]
    public void Route_Composition_Produces_PlexExpected_MovieMatchPath()
    {
        Assert.AreEqual("/movie/library/metadata/matches", $"{ProviderDefinitions.MovieBasePath}{ProviderDefinitions.MatchPath}");
    }

    [TestMethod]
    public void Route_Composition_Produces_PlexExpected_TvMatchPath()
    {
        Assert.AreEqual("/tv/library/metadata/matches", $"{ProviderDefinitions.TvBasePath}{ProviderDefinitions.MatchPath}");
    }

    [TestMethod]
    public void Route_Composition_Produces_PlexExpected_MovieExtrasPath()
    {
        Assert.AreEqual("/movie/library/metadata/{ratingKey}/extras".Replace("{ratingKey}", "movie-tmdb-123"), $"{ProviderDefinitions.MovieBasePath}{ProviderDefinitions.MetadataPath}/movie-tmdb-123{ProviderDefinitions.ExtrasPath}");
    }

    [TestMethod]
    public void Route_Composition_Produces_PlexExpected_TvExtrasPath()
    {
        Assert.AreEqual("/tv/library/metadata/{ratingKey}/extras".Replace("{ratingKey}", "show-tvmaze-123"), $"{ProviderDefinitions.TvBasePath}{ProviderDefinitions.MetadataPath}/show-tvmaze-123{ProviderDefinitions.ExtrasPath}");
    }
}
