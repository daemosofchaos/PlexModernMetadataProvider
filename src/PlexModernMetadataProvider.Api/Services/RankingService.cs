using System.Globalization;
using System.Text;

namespace PlexModernMetadataProvider.Api.Services;

public sealed record CandidateDescriptor<TId>(TId Id, string? Title, string? OriginalTitle, DateOnly? ReleaseDate, double Popularity);

public sealed class RankingService
{
    public IReadOnlyList<CandidateDescriptor<TId>> Rank<TId>(IEnumerable<CandidateDescriptor<TId>> candidates, string queryTitle, int? requestedYear, DateOnly? now = null)
    {
        var today = now ?? DateOnly.FromDateTime(DateTime.UtcNow);

        return candidates
            .OrderByDescending(candidate => TitleScore(queryTitle, candidate.Title, candidate.OriginalTitle))
            .ThenBy(candidate => requestedYear.HasValue ? YearDelta(candidate.ReleaseDate, requestedYear.Value) : DateDistance(candidate.ReleaseDate, today))
            .ThenByDescending(candidate => candidate.ReleaseDate)
            .ThenByDescending(candidate => candidate.Popularity)
            .ThenBy(candidate => candidate.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int TitleScore(string queryTitle, string? title, string? originalTitle)
    {
        var normalizedQuery = Normalize(queryTitle);
        var normalizedTitle = Normalize(title);
        var normalizedOriginal = Normalize(originalTitle);

        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return 0;
        }

        if (normalizedTitle == normalizedQuery || normalizedOriginal == normalizedQuery)
        {
            return 1000;
        }

        if (normalizedTitle.StartsWith(normalizedQuery, StringComparison.Ordinal) || normalizedOriginal.StartsWith(normalizedQuery, StringComparison.Ordinal))
        {
            return 900;
        }

        if (normalizedTitle.Contains(normalizedQuery, StringComparison.Ordinal) || normalizedOriginal.Contains(normalizedQuery, StringComparison.Ordinal))
        {
            return 800;
        }

        var overlap = Math.Max(TokenOverlap(queryTitle, title), TokenOverlap(queryTitle, originalTitle));
        return (int)Math.Round(overlap * 700, MidpointRounding.AwayFromZero);
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
        }

        return string.Join(' ', builder
            .ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static double TokenOverlap(string query, string? candidate)
    {
        var queryTokens = Normalize(query).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.Ordinal);
        var candidateTokens = Normalize(candidate).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.Ordinal);

        if (queryTokens.Count == 0 || candidateTokens.Count == 0)
        {
            return 0;
        }

        var matches = queryTokens.Count(candidateTokens.Contains);
        return matches / (double)Math.Max(queryTokens.Count, candidateTokens.Count);
    }

    private static int YearDelta(DateOnly? releaseDate, int requestedYear)
        => releaseDate.HasValue ? Math.Abs(releaseDate.Value.Year - requestedYear) : int.MaxValue;

    private static int DateDistance(DateOnly? releaseDate, DateOnly today)
        => releaseDate.HasValue ? Math.Abs(releaseDate.Value.DayNumber - today.DayNumber) : int.MaxValue;
}
