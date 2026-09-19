namespace Shoko.Plugin.SampleWebApi.Models;

/// <summary>
/// A few numbers about one series.
/// </summary>
/// <param name="ID">The Shoko series ID.</param>
/// <param name="Title">The series title, in the user's preferred language.</param>
/// <param name="EpisodeCount">How many episodes the series has.</param>
/// <param name="VideoCount">How many videos are linked to it.</param>
public record SeriesSummary(int ID, string Title, int EpisodeCount, int VideoCount);
