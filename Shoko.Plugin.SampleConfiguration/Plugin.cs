using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shoko.Abstractions.Config;
using Shoko.Abstractions.Config.Events;
using Shoko.Abstractions.Plugin;
using Shoko.Abstractions.Plugin.Models;

namespace Shoko.Plugin.SampleConfiguration;

/// <summary>
/// Registers the API client, and advertises a feature only while the
/// configuration can deliver it.
/// </summary>
public class Plugin : IPlugin, IPluginServiceRegistration
{
    private ConfigurationProvider<SampleConfiguration>? _configurationProvider;

    private ILogger<Plugin>? _logger;

    /// <inheritdoc/>
    public Guid ID { get; } = new("7331416c-168b-495c-bb6b-43c38aa4f2f7");

    /// <inheritdoc/>
    public string Name => "Sample: Configuration";

    /// <inheritdoc/>
    public string Description => "A settings page with a secret and a test button, and code that reads the settings where it uses them.";

    /// <inheritdoc/>
    public static void RegisterServices(IServiceCollection serviceCollection, IApplicationPaths applicationPaths)
    {
        // The configuration class needs no registration: the server finds it,
        // and ConfigurationProvider<SampleConfiguration> resolves on its own.
        serviceCollection.AddHttpClient<SampleApiClient>();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The plugin class can't take constructor dependencies, so this is where
    /// it gets its services.
    /// </remarks>
    public void Setup(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<Plugin>>();
        _configurationProvider = serviceProvider.GetRequiredService<ConfigurationProvider<SampleConfiguration>>();
        _configurationProvider.Saved += OnConfigurationSaved;
    }

    /// <inheritdoc/>
    public IReadOnlyList<PluginFeature> GetFeatures()
    {
        // Asked every time a client wants the list, so load the configuration
        // now rather than remembering what it was at startup.
        if (_configurationProvider?.Load() is not { ApiKey.Length: > 0 })
            return [];

        return [new() { Name = "sample-connection" }];
    }

    private void OnConfigurationSaved(object? sender, ConfigurationSavedEventArgs<SampleConfiguration> eventArgs)
    {
        // Raised on a thread-pool task that nothing observes, so an exception
        // escaping this handler would disappear without a log line.
        try
        {
            var state = eventArgs.Configuration.ApiKey is { Length: > 0 } ? "set" : "not set";
            _logger?.LogInformation("The sample configuration was saved. An API key is {State}.", state);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to handle the saved sample configuration.");
        }
    }
}
