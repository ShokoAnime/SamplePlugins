using Shoko.Abstractions.Metadata.Shoko;

namespace Shoko.Plugin.SampleWebApi.Models;

/// <summary>
/// A few numbers about one series.
/// </summary>
/// <param name="ID">The Shoko series ID.</param>
/// <param name="Title">The series title, in the user's preferred language.</param>
/// <param name="EpisodeCount">How many episodes the series has.</param>
/// <param name="VideoCount">How many videos are linked to it.</param>
public record SeriesSummary(int ID, string Title, int EpisodeCount, int VideoCount)
{
    /// <summary>
    /// Summarize a series.
    /// </summary>
    /// <remarks>
    /// <see cref="IShokoSeries.LocalID"/> is the number the Shoko lookups
    /// take. The series' <c>ID</c> is its <c>MetadataGuid</c>, the identity
    /// shared by every metadata source.
    /// </remarks>
    /// <param name="series">The series.</param>
    /// <returns>The summary.</returns>
    public static SeriesSummary FromSeries(IShokoSeries series)
        => new(series.LocalID, series.PreferredTitle?.Value ?? series.Title, series.Episodes.Count, series.Videos.Count);
}
