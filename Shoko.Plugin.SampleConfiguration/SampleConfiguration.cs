using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shoko.Abstractions.Config;
using Shoko.Abstractions.Config.Attributes;
using Shoko.Abstractions.Config.Enums;

namespace Shoko.Plugin.SampleConfiguration;

/// <summary>
/// Connection settings for a made-up remote service. The descriptions on this
/// page come from the XML summaries in the source, which is why the project
/// generates its documentation file.
/// </summary>
[Display(Name = "Sample Configuration")]
public class SampleConfiguration : IConfiguration
{
    /// <summary>
    /// The address of the remote service.
    /// </summary>
    [SectionName("Connection")]
    [Display(Name = "Server Address")]
    public string ServerAddress { get; set; } = "https://example.com/";

    /// <summary>
    /// Your personal API key for the remote service. Nothing is sent until one
    /// is set.
    /// </summary>
    /// <remarks>
    /// Marked as a secret, so the settings page shows a password field and the
    /// API never sends the stored value back out. It is deliberately not
    /// <c>[Required]</c>: that would fail every load of this configuration
    /// until the user saved a key, which is the state every new install starts
    /// in. Check for it where it is used instead.
    /// </remarks>
    [SectionName("Connection")]
    [Display(Name = "API Key")]
    [PasswordPropertyText]
    public string? ApiKey { get; set; }

    /// <summary>
    /// How long to wait for the remote service before giving up.
    /// </summary>
    [Display(Name = "Request Timeout (Seconds)")]
    [Range(1, 120)]
    [DefaultValue(30)]
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Checks that the server address can be reached with the API key as it is
    /// on screen, before it is saved.
    /// </summary>
    /// <param name="context">The action context, carrying the unsaved configuration.</param>
    /// <returns>A message for the settings page.</returns>
    [CustomAction(Theme = DisplayColorTheme.Primary, Position = DisplayButtonPosition.Top, SectionName = "Connection")]
    [Display(Name = "Test Connection")]
    public async Task<ConfigurationActionResult> TestConnection(ConfigurationActionContext<SampleConfiguration> context)
    {
        if (context.Configuration.ApiKey is not { Length: > 0 } apiKey)
            return new("Enter an API key first.", DisplayColorTheme.Warning);

        // The context holds whatever is on screen, saved or not. Take the key
        // from it, since testing a new key is the point of the button, but
        // take the address from the saved configuration, so a half-typed
        // address can't send the key somewhere the user never meant it to go.
        var saved = context.ConfigurationService.Load<SampleConfiguration>();
        if (!Uri.TryCreate(saved.ServerAddress, UriKind.Absolute, out var serverAddress))
            return new("The saved server address is not a valid URL.", DisplayColorTheme.Warning);

        try
        {
            // Custom actions are synchronous, so the call is waited on here.
            var client = context.PluginManager.GetRequiredService<SampleApiClient>();
            return await client.CheckConnection(serverAddress, apiKey)
                ? new($"Connected to {serverAddress.Host}.", DisplayColorTheme.Important)
                : new($"{serverAddress.Host} rejected the API key.", DisplayColorTheme.Warning);
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "The connection test against {ServerAddress} failed.", serverAddress);
            return new($"Could not reach {serverAddress.Host}.", DisplayColorTheme.Danger);
        }
    }
}
