using System;
using Shoko.Abstractions.Plugin;

namespace Shoko.Plugin.OriginalNameRenamer;

/// <summary>
/// The plugin's identity. The relocation provider next to it is found by the
/// server on its own, so this class has nothing else to do.
/// </summary>
public class Plugin : IPlugin
{
    /// <inheritdoc/>
    /// <remarks>
    /// Must match the <c>id</c> in <c>manifest.json</c>. Generate a new one for
    /// your own plugin, and never change it afterwards.
    /// </remarks>
    public Guid ID { get; } = new("b0441b1b-2c17-4004-95e2-4f89ce776e55");

    /// <inheritdoc/>
    public string Name => "Sample: Original Name Renamer";

    /// <inheritdoc/>
    public string Description => "Renames files to the name they were released under.";
}
