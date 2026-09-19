using System;
using Microsoft.AspNetCore.Builder;
using Shoko.Abstractions.Plugin;
using Shoko.Plugin.SampleWebApi.Hubs;

namespace Shoko.Plugin.SampleWebApi;

/// <summary>
/// Maps the plugin's SignalR hub. Controllers and actions need no
/// registration: the server finds both in the plugin assembly.
/// </summary>
public class Plugin : IPlugin, IPluginApplicationRegistration
{
    /// <summary>
    /// The one namespace everything this plugin serves lives under:
    /// <c>/api/plugin/SampleWebApi/…</c> for the API,
    /// <c>/plugin/SampleWebApi/…</c> for pages and assets, and
    /// <c>/signalr/plugin/SampleWebApi/…</c> for hubs. No other plugin may
    /// use the same one, and nothing checks that for you.
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
    public string Description => "Serves its own API endpoints and SignalR hub, and adds actions users can run from the Web UI.";

    /// <inheritdoc/>
    public static void RegisterServices(IApplicationBuilder application, IApplicationPaths applicationPaths)
    {
        // Called while the request pipeline is built, after authentication,
        // so the hub's [Authorize] is honoured.
        application.UseEndpoints(endpoints => endpoints.MapHub<EventsHub>($"/signalr/plugin/{RouteNamespace}/events"));
    }
}
