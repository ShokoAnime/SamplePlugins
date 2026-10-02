using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shoko.Abstractions.Plugin;
using Shoko.Abstractions.Web.SignalR;
using Shoko.Plugin.SampleWebApi.Feeds;
using Shoko.Plugin.SampleWebApi.Hubs;

namespace Shoko.Plugin.SampleWebApi;

/// <summary>
/// Registers the plugin's feed and maps its SignalR hub. Controllers and
/// actions need no registration: the server finds both in the plugin
/// assembly.
/// </summary>
public class Plugin : IPlugin, IPluginServiceRegistration, IPluginApplicationRegistration
{
    /// <summary>
    /// The one namespace everything this plugin serves lives under:
    /// <c>/api/plugin/SampleWebApi/…</c> for the API,
    /// <c>/plugin/SampleWebApi/…</c> for pages and assets, and
    /// <c>/signalr/plugin/SampleWebApi/…</c> for hubs. The feed on the
    /// aggregate hub takes the same name. No other plugin may use the same
    /// one, and nothing checks that for you.
    /// </summary>
    public const string RouteNamespace = "SampleWebApi";

    /// <summary>
    /// The plugin ID, as a constant so the controllers can derive action IDs
    /// from it.
    /// </summary>
    public const string PluginID = "916cc80b-5d58-4a3f-9b85-da74e457d5dd";

    /// <inheritdoc/>
    public Guid ID { get; } = new(PluginID);

    /// <inheritdoc/>
    public string Name => "Sample: Web API and Actions";

    /// <inheritdoc/>
    public string Description => "Serves its own API endpoints and SignalR hub, sends events to a feed, and adds actions users can run from the Web UI.";

    /// <inheritdoc/>
    public static void RegisterServices(IServiceCollection serviceCollection, IApplicationPaths applicationPaths)
    {
        // Registers the feed as itself and as an IEventEmitter, so the
        // aggregate hub and the plugin's own services share one instance.
        serviceCollection.AddEventEmitter<SampleWebApiFeed>();
    }

    /// <inheritdoc/>
    public static void RegisterServices(IApplicationBuilder application, IApplicationPaths applicationPaths)
    {
        // Called while the request pipeline is built, after authentication,
        // so the hub's [Authorize] is honoured.
        application.UseEndpoints(endpoints => endpoints.MapHub<SeriesHub>($"/signalr/plugin/{RouteNamespace}/series"));
    }
}
