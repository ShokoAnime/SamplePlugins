namespace Shoko.Plugin.SampleDashboard.Models;

/// <summary>
/// What the dashboard shows.
/// </summary>
/// <param name="UserName">The name of the user asking.</param>
/// <param name="SeriesCount">How many series are in the library.</param>
/// <param name="VideoCount">How many videos are in the library.</param>
public record DashboardStats(string UserName, int SeriesCount, int VideoCount);
