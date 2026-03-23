using PlexModernMetadataProvider.Api.Services;

namespace PlexModernMetadataProvider.Tests.Services;

[TestClass]
public sealed class RankingServiceTests
{
    private readonly RankingService _rankingService = new();

    [TestMethod]
    public void Rank_PrefersNearestCurrentDateWhenNoYearExists()
    {
        var candidates = new[]
        {
            new CandidateDescriptor<int>(1, "Rooster", null, new DateOnly(2013, 5, 1), 10),
            new CandidateDescriptor<int>(2, "Rooster", null, new DateOnly(2026, 2, 1), 5)
        };

        var ranked = _rankingService.Rank(candidates, "Rooster", null, new DateOnly(2026, 3, 23));

        Assert.AreEqual(2, ranked[0].Id);
    }

    [TestMethod]
    public void Rank_PrefersRequestedYearEvenIfPopularityIsHigherElsewhere()
    {
        var candidates = new[]
        {
            new CandidateDescriptor<int>(1, "Rooster", null, new DateOnly(2013, 5, 1), 10),
            new CandidateDescriptor<int>(2, "Rooster", null, new DateOnly(2026, 2, 1), 100)
        };

        var ranked = _rankingService.Rank(candidates, "Rooster", 2013, new DateOnly(2026, 3, 23));

        Assert.AreEqual(1, ranked[0].Id);
    }

    [TestMethod]
    public void Rank_PrefersExactTitleOverLooserMatch()
    {
        var candidates = new[]
        {
            new CandidateDescriptor<int>(1, "Rooster", null, new DateOnly(2013, 5, 1), 10),
            new CandidateDescriptor<int>(2, "The Rooster Chronicles", null, new DateOnly(2026, 2, 1), 100)
        };

        var ranked = _rankingService.Rank(candidates, "Rooster", null, new DateOnly(2026, 3, 23));

        Assert.AreEqual(1, ranked[0].Id);
    }
}
