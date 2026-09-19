using System;
using Shoko.Abstractions.Plugin;

namespace Shoko.Plugin.SampleProviders;

/// <summary>
/// The plugin's identity. The two providers are found by the server on
/// their own: implementing the contract on a public class is the whole of
/// it, with nothing to register.
/// </summary>
public class Plugin : IPlugin
{
    /// <inheritdoc/>
    public Guid ID { get; } = new("0cf2ea5a-43d0-4cf6-ac33-b0ae87e777b2");

    /// <inheritdoc/>
    public string Name => "Sample: Providers";

    /// <inheritdoc/>
    public string Description => "A release info provider that reads AniDB episode IDs from file names, and a hash provider for the OpenSubtitles hash.";
}
