using System.IO;
using System.Text.RegularExpressions;

namespace PlexModernMetadataProvider.Api.Services;

public sealed record ParsedFilename(string? Title, int? Year, int? Season, int? Episode, string? AirDate);

public sealed partial class FilenameParser
{
    private static readonly Regex[] NoisePatterns =
    [
        new Regex(@"\b(480p|720p|1080p|2160p|4k|8k)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\b(web[- .]?dl|webrip|bluray|bdrip|dvdrip|hdrip|remux|x264|x265|h264|h265|hevc|aac|dts|atmos)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\b(proper|repack|extended|limited|uncut|multi|dubbed|subbed)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];

    public ParsedFilename Parse(string? filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return new ParsedFilename(null, null, null, null, null);
        }

        var raw = Path.GetFileNameWithoutExtension(filename);
        var airDateMatch = AirDateRegex().Match(raw);
        var seasonEpisodeMatch = SeasonEpisodeRegex().Match(raw);
        var yearMatch = YearRegex().Match(raw);

        var yearInsideAirDate = airDateMatch.Success
            && yearMatch.Success
            && yearMatch.Groups[1].Index >= airDateMatch.Index
            && yearMatch.Groups[1].Index < airDateMatch.Index + airDateMatch.Length;

        int? year = yearMatch.Success && !yearInsideAirDate
            ? int.Parse(yearMatch.Groups[1].Value)
            : null;

        int cutIndex = raw.Length;
        if (seasonEpisodeMatch.Success)
        {
            cutIndex = Math.Min(cutIndex, seasonEpisodeMatch.Index);
        }

        if (airDateMatch.Success)
        {
            cutIndex = Math.Min(cutIndex, airDateMatch.Index);
        }
        else if (yearMatch.Success && !yearInsideAirDate)
        {
            cutIndex = Math.Min(cutIndex, yearMatch.Index);
        }

        var titlePart = raw[..cutIndex];
        titlePart = titlePart.Replace('.', ' ').Replace('_', ' ');
        titlePart = TrimBracketedSuffixRegex().Replace(titlePart, " ");

        foreach (var pattern in NoisePatterns)
        {
            titlePart = pattern.Replace(titlePart, " ");
        }

        titlePart = MultiSpaceRegex().Replace(titlePart, " ").Trim();

        var season = seasonEpisodeMatch.Success
            ? int.Parse(seasonEpisodeMatch.Groups[1].Success ? seasonEpisodeMatch.Groups[1].Value : seasonEpisodeMatch.Groups[3].Value)
            : (int?)null;

        var episode = seasonEpisodeMatch.Success
            ? int.Parse(seasonEpisodeMatch.Groups[2].Success ? seasonEpisodeMatch.Groups[2].Value : seasonEpisodeMatch.Groups[4].Value)
            : (int?)null;

        var airDate = airDateMatch.Success
            ? $"{airDateMatch.Groups[1].Value}-{airDateMatch.Groups[2].Value}-{airDateMatch.Groups[3].Value}"
            : null;

        return new ParsedFilename(titlePart.Length == 0 ? null : titlePart, year, season, episode, airDate);
    }

    [GeneratedRegex(@"(?:\bS(\d{1,2})E(\d{1,3})\b)|(?:\b(\d{1,2})x(\d{1,3})\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SeasonEpisodeRegex();

    [GeneratedRegex(@"(?:^|[ ._\-(])((?:19|20)\d{2})(?=$|[ ._\-)])", RegexOptions.Compiled)]
    private static partial Regex YearRegex();

    [GeneratedRegex(@"\b((?:19|20)\d{2})[ ._-]?((?:0[1-9]|1[0-2]))[ ._-]?((?:0[1-9]|[12]\d|3[01]))\b", RegexOptions.Compiled)]
    private static partial Regex AirDateRegex();

    [GeneratedRegex(@"\[[^\]]*\]|\([^)]*\)$", RegexOptions.Compiled)]
    private static partial Regex TrimBracketedSuffixRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex MultiSpaceRegex();
}
