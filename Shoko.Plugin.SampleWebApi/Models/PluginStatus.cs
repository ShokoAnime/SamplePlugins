namespace Shoko.Plugin.SampleWebApi.Models;

/// <summary>
/// What state the server is in, as far as this plugin can tell.
/// </summary>
/// <param name="Version">The plugin version.</param>
/// <param name="IsStarted">Whether the server has finished starting.</param>
/// <param name="InSetupMode">Whether the first-run setup is still waiting to be completed.</param>
/// <param name="IsDatabaseBlocked">Whether the database is blocked.</param>
public record PluginStatus(string Version, bool IsStarted, bool InSetupMode, bool IsDatabaseBlocked);
