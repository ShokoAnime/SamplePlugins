using System.Threading.Tasks;
using Shoko.Abstractions.Web.SignalR;
using Shoko.Plugin.SampleWebApi.Models;

namespace Shoko.Plugin.SampleWebApi.Feeds;

/// <summary>
/// The plugin's feed on the server's aggregate hub, at
/// <c>/signalr/aggregate</c>. Clients join it by the plugin's namespace and
/// receive <c>SampleWebApi:series.summarized</c> with a
/// <see cref="SeriesSummary"/>.
/// </summary>
/// <remarks>
/// Pushing to clients needs no hub of the plugin's own: clients already keep
/// a connection to the aggregate hub, and join and leave feeds on it.
/// Registered with <c>AddEventEmitter</c>, so it is a singleton that any
/// service of the plugin can inject and send through.
/// </remarks>
public class SampleWebApiFeed : EventEmitter
{
    /// <inheritdoc/>
    /// <remarks>
    /// Named after the plugin, since nothing prefixes a feed's name and two
    /// feeds of the same name clash. Names are matched ignoring case.
    /// </remarks>
    public override string Name => Plugin.RouteNamespace;

    /// <summary>
    /// Tell every client on the feed that a series was summarized.
    /// </summary>
    /// <param name="summary">The summary.</param>
    /// <returns>A task that completes once the message is sent.</returns>
    public Task SendSeriesSummarized(SeriesSummary summary)
        => SendAsync("series.summarized", summary);
}
