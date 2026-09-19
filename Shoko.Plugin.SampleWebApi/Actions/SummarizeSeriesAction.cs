using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Shoko.Abstractions.Actions;
using Shoko.Plugin.SampleWebApi.Hubs;

namespace Shoko.Plugin.SampleWebApi.Actions;

/// <summary>
/// A series action: offered on every series, and handed the series it runs on.
/// </summary>
/// <remarks>
/// Scoped actions must derive from <see cref="SeriesAction"/> (or the group,
/// episode or video equivalent) directly. An intermediate base class of your
/// own fails the server's start-up.
/// </remarks>
/// <param name="hubContext">Used to tell connected clients the summary is done.</param>
/// <param name="logger">The logger.</param>
public class SummarizeSeriesAction(IHubContext<EventsHub> hubContext, ILogger<SummarizeSeriesAction> logger) : SeriesAction
{
    /// <inheritdoc/>
    public override string Name => "Summarize Series";

    /// <inheritdoc/>
    public override string Description => "Writes a short summary of the series to the server log.";

    /// <inheritdoc/>
    public override ActionCategory Category => ActionCategory.PluginInferred;

    /// <inheritdoc/>
    public override ActionPermission Permission => ActionPermission.User;

    /// <inheritdoc/>
    /// <remarks>
    /// Runs on the request thread, on a different instance than
    /// <see cref="Execute"/>, so keep it a cheap check. Returning a result
    /// refuses the run, and the API answers <c>400</c> with the reason.
    /// </remarks>
    public override Task<ActionValidationResult?> Validate(CancellationToken token = default)
        => Task.FromResult(Series.Videos.Count is 0 ? new ActionValidationResult($"\"{Series.Title}\" has no files to summarize.") : null);

    /// <inheritdoc/>
    public override async Task Execute(CancellationToken token = default)
    {
        logger.LogInformation("{Title}: {EpisodeCount} episodes, {VideoCount} videos.", Series.Title, Series.Episodes.Count, Series.Videos.Count);
        await hubContext.Clients.All.SendAsync("SeriesSummarized", Series.ID, Series.Title, token).ConfigureAwait(false);
    }
}
