using System.Text.Json;
using System.Text.Json.Serialization;
using PlexModernMetadataProvider.Api.Models;

namespace PlexModernMetadataProvider.Tests.Services;

[TestClass]
public sealed class PlexSerializationTests
{
    [TestMethod]
    public void MovieMetadataItem_Serializes_guid_And_Guid_WithoutCollision()
    {
        var item = new MovieMetadataItem
        {
            RatingKey = "movie:omdb:tt1234567",
            Key = "/movie/library/metadata/movie:omdb:tt1234567",
            Guid = "plex://movie/movie:omdb:tt1234567",
            Title = "Example Movie",
            OriginallyAvailableAt = "2026-01-01",
            ExtensionData = new Dictionary<string, object?>
            {
                ["Guid"] = new List<PlexGuid> { new() { Id = "omdb://tt1234567" } },
                ["Studio"] = new List<PlexTag> { new() { Tag = "Example Studio" } }
            }
        };

        var json = JsonSerializer.Serialize(item, new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        StringAssert.Contains(json, "\"guid\":\"plex://movie/movie:omdb:tt1234567\"");
        StringAssert.Contains(json, "\"Guid\":[{");
        StringAssert.Contains(json, "\"Studio\":[{");
    }
}
