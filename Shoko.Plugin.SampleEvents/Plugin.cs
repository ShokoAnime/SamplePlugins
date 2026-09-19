using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shoko.Abstractions.Plugin;

namespace Shoko.Plugin.SampleEvents;

/// <summary>
/// Shows where each part of a plugin's start-up goes.
/// </summary>
/// <remarks>
/// The server builds this class twice, the first time without a service
/// container, so it must keep its public parameterless constructor. Anything
/// it needs comes in through <see cref="Setup"/>.
/// </remarks>
public class Plugin : IPlugin, IPluginServiceRegistration
{
    private ILogger<Plugin>? _logger;

    private IPluginManager? _pluginManager;

    /// <inheritdoc/>
    public Guid ID { get; } = new("4b7280b2-af12-4e0b-afc6-b599aa02dae5");

    /// <inheritdoc/>
    public string Name => "Sample: Events and Lifecycle";

    /// <inheritdoc/>
    public string Description => "Reacts to library events without slowing the server down, and hands the work to a queue job.";

    /// <inheritdoc/>
    /// <remarks>
    /// Runs while the container is still being built, so register here and
    /// resolve nothing. The queue job needs no registration: every plugin
    /// assembly is scanned for jobs.
    /// </remarks>
    public static void RegisterServices(IServiceCollection serviceCollection, IApplicationPaths applicationPaths)
    {
        // Event subscriptions live in a hosted service, which starts and stops
        // with the server and so has somewhere to unsubscribe.
        serviceCollection.AddHostedService<LibraryEventListener>();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Runs once every plugin is initialized, before the database is open.
    /// Take services here and leave the work that uses them for later. A
    /// throw stops the server finishing its start-up.
    /// </remarks>
    public void Setup(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<Plugin>>();
        _pluginManager = serviceProvider.GetRequiredService<IPluginManager>();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Runs once every plugin has run <see cref="Setup"/>, still before the
    /// database is open. A plugin that collects contributions from other
    /// plugins during their <see cref="Setup"/> freezes them here. This one
    /// only reports what it can see.
    /// </remarks>
    public void Ready()
    {
        var activePlugins = _pluginManager?.GetPluginInfos().Count(info => info.IsActive) ?? 0;
        _logger?.LogInformation("Every plugin is set up; {Count} are active.", activePlugins);
    }
}
