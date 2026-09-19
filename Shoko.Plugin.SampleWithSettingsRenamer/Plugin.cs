using System;
using Shoko.Abstractions.Plugin;

namespace Shoko.Plugin.SampleWithSettingsRenamer;

/// <summary>
/// The plugin's identity.
/// </summary>
public class Plugin : IPlugin
{
    /// <inheritdoc/>
    public Guid ID { get; } = new("62c4b8eb-57f5-4c41-9818-17eec3e5c3be");

    /// <inheritdoc/>
    public string Name => "Sample: Renamer With Settings";

    /// <inheritdoc/>
    public string Description => "Renames files to a fixed format and sorts them into group and series folders, with per-preset settings.";
}
