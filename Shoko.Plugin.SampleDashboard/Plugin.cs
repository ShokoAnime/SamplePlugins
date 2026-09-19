using System;
using System.Collections.Generic;
using Shoko.Abstractions.Plugin;
using Shoko.Abstractions.Plugin.Models;

namespace Shoko.Plugin.SampleDashboard;

/// <summary>
/// Offers the dashboard to the Web UI as a plugin page.
/// </summary>
public class Plugin : IPlugin
{
    /// <summary>
    /// The one namespace everything this plugin serves lives under: the page
    /// at <c>/plugin/SampleDashboard/</c> and its API at
    /// <c>/api/plugin/SampleDashboard/…</c>.
    /// </summary>
    public const string RouteNamespace = "SampleDashboard";

    /// <inheritdoc/>
    public Guid ID { get; } = new("616d29bb-c4ab-48e7-9ac2-e16d3c749a92");

    /// <inheritdoc/>
    public string Name => "Sample: Dashboard";

    /// <inheritdoc/>
    public string Description => "A web page built with Vite, compressed into the plugin at build time, and served by the plugin with its own API.";

    /// <inheritdoc/>
    /// <remarks>
    /// The Web UI shows the page in a frame. The page shares the Web UI's
    /// origin, and with it the signed-in session.
    /// </remarks>
    public IReadOnlyList<PluginPage> GetPages()
        => [new() { Name = "Dashboard", Url = $"/plugin/{RouteNamespace}/" }];
}
