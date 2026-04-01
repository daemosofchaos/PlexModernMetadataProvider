using System.IO;
using System.Text.RegularExpressions;

namespace PlexModernMetadataProvider.Api.Services;

public sealed record ParsedFilename(string? Title, int? Year, int? Season, int? Episode, string? AirDate);

public sealed partial class FilenameParser
{
    private static readonly Regex[] NoisePatterns =
    [
        new Regex(@"\b(480p|576p|720p|1080p|1440p|2160p|4320p|4k|8k)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\b(web[- .]?dl|web[- .]?rip|webrip|bluray|blu[- ]?ray|bdrip|dvdrip|hdrip|uhd|remux|x264|x265|h264|h265|hevc|av1)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\b(ddp?\s?\d(?:\.\d)?|ac3|eac3|aac(?:2\.0)?|dts(?:[- ]?hd)?|truehd|atmos|flac|mp3)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\b(hdr10\+?|hdr|sdr|dolby[ -]?vision|dv|imax|proper|repack|extended|limited|uncut|remastered|criterion|multi|dubbed|subbed)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];

    private static readonly HashSet<string> IgnoredFolderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "movies", "movie", "films", "tv", "shows", "show", "tv shows", "television", "series", "anime", "cartoons"
    };

    public ParsedFilename Parse(string? filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return new ParsedFilename(null, null, null, null, null);
        }

        var rawPath = filename.Trim();
        var raw = Path.GetFileNameWithoutExtension(rawPath);

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

        var fileTitle = ExtractTitleCandidate(raw);
        var folderTitle = ExtractFolderTitle(rawPath, seasonEpisodeMatch.Success || airDateMatch.Success);
        var title = ChooseBestTitle(fileTitle, folderTitle, raw);

        var season = seasonEpisodeMatch.Success
            ? int.Parse(seasonEpisodeMatch.Groups[1].Success ? seasonEpisodeMatch.Groups[1].Value : seasonEpisodeMatch.Groups[3].Value)
            : (int?)null;

        var episode = seasonEpisodeMatch.Success
            ? int.Parse(seasonEpisodeMatch.Groups[2].Success ? seasonEpisodeMatch.Groups[2].Value : seasonEpisodeMatch.Groups[4].Value)
            : (int?)null;

        var airDate = airDateMatch.Success
            ? $"{airDateMatch.Groups[1].Value}-{airDateMatch.Groups[2].Value}-{airDateMatch.Groups[3].Value}"
            : null;

        return new ParsedFilename(title, year, season, episode, airDate);
    }

    private static string? ExtractFolderTitle(string rawPath, bool isSeriesLike)
    {
        var directoryName = Path.GetDirectoryName(rawPath);
        if (string.IsNullOrWhiteSpace(directoryName))
        {
            return null;
        }

        var segments = directoryName
            .Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Reverse()
            .ToArray();

        foreach (var segment in segments)
        {
            if (IgnoredFolderNames.Contains(segment) || LooksLikeSeasonFolder(segment))
            {
                continue;
            }

            var candidate = ExtractTitleCandidate(segment);
            if (string.IsNullOrWhiteSpace(candidate) || IgnoredFolderNames.Contains(candidate))
            {
                continue;
            }

            return candidate;
        }

        return isSeriesLike ? null : ExtractTitleCandidate(Path.GetFileName(directoryName));
    }

    private static string? ChooseBestTitle(string? fileTitle, string? folderTitle, string raw)
    {
        if (string.IsNullOrWhiteSpace(fileTitle))
        {
            return folderTitle;
        }

        if (string.IsNullOrWhiteSpace(folderTitle))
        {
            return fileTitle;
        }

        if (HasJunkPrefix(raw) || fileTitle.StartsWith("www ", StringComparison.OrdinalIgnoreCase))
        {
            return folderTitle;
        }

        return fileTitle;
    }

    private static bool HasJunkPrefix(string value)
        => LeadingSitePrefixRegex().IsMatch(NormalizeSeparators(value));

    private static bool LooksLikeSeasonFolder(string value)
        => SeasonFolderRegex().IsMatch(NormalizeSeparators(value));

    private static string? ExtractTitleCandidate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var airDateMatch = AirDateRegex().Match(raw);
        var seasonEpisodeMatch = SeasonEpisodeRegex().Match(raw);
        var yearMatch = YearRegex().Match(raw);

        var yearInsideAirDate = airDateMatch.Success
            && yearMatch.Success
            && yearMatch.Groups[1].Index >= airDateMatch.Index
            && yearMatch.Groups[1].Index < airDateMatch.Index + airDateMatch.Length;

        var cutIndex = raw.Length;
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

        var candidate = NormalizeSeparators(raw[..cutIndex]);
        candidate = LeadingBracketedPrefixRegex().Replace(candidate, " ");
        candidate = LeadingSitePrefixRegex().Replace(candidate, string.Empty);
        candidate = TrimBracketedSuffixRegex().Replace(candidate, " ");
        candidate = ReleaseGroupSuffixRegex().Replace(candidate, " ");

        foreach (var pattern in NoisePatterns)
        {
            candidate = pattern.Replace(candidate, " ");
        }

        candidate = PunctuationCleanupRegex().Replace(candidate, " ");
        candidate = MultiSpaceRegex().Replace(candidate, " ").Trim(' ', '-', '.', '_');

        return candidate.Length == 0 ? null : candidate;
    }

    private static string NormalizeSeparators(string value)
        => value.Replace('.', ' ').Replace('_', ' ');

    [GeneratedRegex(@"(?:\bS(\d{1,2})E(\d{1,3})\b)|(?:\b(\d{1,2})x(\d{1,3})\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SeasonEpisodeRegex();

    [GeneratedRegex(@"(?:^|[ ._\-(])((?:19|20)\d{2})(?=$|[ ._\-)])", RegexOptions.Compiled)]
    private static partial Regex YearRegex();

    [GeneratedRegex(@"\b((?:19|20)\d{2})[ ._-]?((?:0[1-9]|1[0-2]))[ ._-]?((?:0[1-9]|[12]\d|3[01]))\b", RegexOptions.Compiled)]
    private static partial Regex AirDateRegex();

    [GeneratedRegex(@"\[[^\]]*\]|\([^)]*\)$", RegexOptions.Compiled)]
    private static partial Regex TrimBracketedSuffixRegex();

    [GeneratedRegex(@"^\s*\[[^\]]*\]\s*[-–—:]?\s*", RegexOptions.Compiled)]
    private static partial Regex LeadingBracketedPrefixRegex();

    [GeneratedRegex(@"^\s*(?:www\s+)?(?:[a-z0-9-]+\s+){0,4}(?:org|com|net|info|io|co|me|tv)\s*[-–—:]+\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex LeadingSitePrefixRegex();

    [GeneratedRegex(@"\s+-\s+[A-Za-z0-9][A-Za-z0-9-]{1,20}$", RegexOptions.Compiled)]
    private static partial Regex ReleaseGroupSuffixRegex();

    [GeneratedRegex(@"^(?:season|specials?|extras?|bonus|saison|staffel|temporada|series?)\s*\d{0,4}$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SeasonFolderRegex();

    [GeneratedRegex(@"[^\p{L}\p{Nd}\s&'’-]+", RegexOptions.Compiled)]
    private static partial Regex PunctuationCleanupRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex MultiSpaceRegex();
}
