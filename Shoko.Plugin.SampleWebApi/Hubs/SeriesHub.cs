using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Shoko.Abstractions.Metadata.Services;
using Shoko.Plugin.SampleWebApi.Models;

namespace Shoko.Plugin.SampleWebApi.Hubs;

/// <summary>
/// Answers clients that call into the plugin over SignalR. Clients
/// authenticate with their API key as a bearer token, or as the
/// <c>access_token</c> query parameter.
/// </summary>
/// <remarks>
/// A hub of the plugin's own is for calls from the client. Messages to
/// clients go through <see cref="Feeds.SampleWebApiFeed"/> on the aggregate
/// hub instead. A hub instance lives for one call, so it keeps no state.
/// </remarks>
/// <param name="metadataService">Used to look series up.</param>
[Authorize]
public class SeriesHub(IMetadataService metadataService) : Hub
{
    /// <summary>
    /// Get a summary of one series.
    /// </summary>
    /// <param name="seriesID">The Shoko series ID.</param>
    /// <returns>The summary, or <see langword="null"/> if there is no such series.</returns>
    public SeriesSummary? GetSummary(int seriesID)
        => metadataService.GetShokoSeriesByID(seriesID) is { } series ? SeriesSummary.FromSeries(series) : null;
}
