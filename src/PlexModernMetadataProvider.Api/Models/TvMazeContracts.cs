using System.Text.Json.Serialization;

namespace PlexModernMetadataProvider.Api.Models;

public sealed class TvMazeSearchResult
{
    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("show")]
    public TvMazeShow? Show { get; set; }
}

public sealed class TvMazeShow
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("genres")]
    public List<string>? Genres { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("runtime")]
    public int? Runtime { get; set; }

    [JsonPropertyName("averageRuntime")]
    public int? AverageRuntime { get; set; }

    [JsonPropertyName("premiered")]
    public string? Premiered { get; set; }

    [JsonPropertyName("ended")]
    public string? Ended { get; set; }

    [JsonPropertyName("officialSite")]
    public string? OfficialSite { get; set; }

    [JsonPropertyName("rating")]
    public TvMazeRating? Rating { get; set; }

    [JsonPropertyName("network")]
    public TvMazeNetwork? Network { get; set; }

    [JsonPropertyName("webChannel")]
    public TvMazeNetwork? WebChannel { get; set; }

    [JsonPropertyName("externals")]
    public TvMazeExternals? Externals { get; set; }

    [JsonPropertyName("image")]
    public TvMazeImage? Image { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}

public sealed class TvMazeEpisode
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("season")]
    public int Season { get; set; }

    [JsonPropertyName("number")]
    public int? Number { get; set; }

    [JsonPropertyName("airdate")]
    public string? AirDate { get; set; }

    [JsonPropertyName("runtime")]
    public int? Runtime { get; set; }

    [JsonPropertyName("rating")]
    public TvMazeRating? Rating { get; set; }

    [JsonPropertyName("image")]
    public TvMazeImage? Image { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}

public sealed class TvMazeCastEntry
{
    [JsonPropertyName("person")]
    public TvMazePerson? Person { get; set; }

    [JsonPropertyName("character")]
    public TvMazeCharacter? Character { get; set; }

    [JsonPropertyName("self")]
    public bool? Self { get; set; }

    [JsonPropertyName("voice")]
    public bool? Voice { get; set; }
}

public sealed class TvMazePerson
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("image")]
    public TvMazeImage? Image { get; set; }
}

public sealed class TvMazeCharacter
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("image")]
    public TvMazeImage? Image { get; set; }
}

public sealed class TvMazeImage
{
    [JsonPropertyName("medium")]
    public string? Medium { get; set; }

    [JsonPropertyName("original")]
    public string? Original { get; set; }
}

public sealed class TvMazeRating
{
    [JsonPropertyName("average")]
    public double? Average { get; set; }
}

public sealed class TvMazeNetwork
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("country")]
    public TvMazeCountry? Country { get; set; }
}

public sealed class TvMazeCountry
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

public sealed class TvMazeExternals
{
    [JsonPropertyName("imdb")]
    public string? Imdb { get; set; }

    [JsonPropertyName("thetvdb")]
    public int? TheTvDb { get; set; }
}
