using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PlexModernMetadataProvider.Api.Options;
using PlexModernMetadataProvider.Api.Services;

namespace PlexModernMetadataProvider.Tests.Services;

[TestClass]
public sealed class PlexReconciliationServiceTests
{
    [TestMethod]
    public async Task FindExistingShowAsync_Returns_ExactTitleAndYearMatch()
    {
        const string xml = """
<MediaContainer size="2" totalSize="2">
  <Directory type="show" ratingKey="101" title="Deadliest Catch" year="2005" guid="plex://show/deadliest-catch">
    <Guid id="tmdb://41790" />
    <Guid id="tvdb://79349" />
  </Directory>
  <Directory type="show" ratingKey="102" title="Deadliest Catch" year="2015" guid="plex://show/deadliest-catch-2015">
    <Guid id="tmdb://99999" />
  </Directory>
</MediaContainer>
""";

        var service = CreateService(xml);
        var result = await service.FindExistingShowAsync("Deadliest Catch", 2005);

        Assert.IsNotNull(result);
        Assert.AreEqual("Deadliest Catch", result.Title);
        Assert.AreEqual(2005, result.Year);
        CollectionAssert.AreEquivalent(
            new[] { "tmdb://41790", "tvdb://79349" },
            result.ExternalIds.Select(item => $"{item.Provider}://{item.Id}").ToArray());
    }

    [TestMethod]
    public async Task FindExistingShowAsync_ReturnsNull_When_NoYearAndMultipleExactMatchesExist()
    {
        const string xml = """
<MediaContainer size="2" totalSize="2">
  <Directory type="show" ratingKey="101" title="Rooster" year="2013" guid="plex://show/rooster-2013">
    <Guid id="tmdb://111" />
  </Directory>
  <Directory type="show" ratingKey="102" title="Rooster" year="2026" guid="plex://show/rooster-2026">
    <Guid id="tmdb://222" />
  </Directory>
</MediaContainer>
""";

        var service = CreateService(xml);
        var result = await service.FindExistingShowAsync("Rooster", null);

        Assert.IsNull(result);
    }

    private static PlexReconciliationService CreateService(string xml)
    {
        var httpClient = new HttpClient(new StaticXmlHandler(xml))
        {
            BaseAddress = new Uri("http://127.0.0.1:32400/")
        };

        var options = Options.Create(new ProviderOptions
        {
            Plex = new PlexOptions
            {
                EnableReconciliation = true,
                BaseUrl = "http://127.0.0.1:32400",
                Token = "test-token",
                RequestTimeoutSeconds = 10,
                CacheTtlMinutes = 5
            }
        });

        return new PlexReconciliationService(
            httpClient,
            new MemoryCache(new MemoryCacheOptions()),
            options,
            NullLogger<PlexReconciliationService>.Instance);
    }

    private sealed class StaticXmlHandler(string xml) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(xml, Encoding.UTF8, "application/xml")
            });
    }
}
