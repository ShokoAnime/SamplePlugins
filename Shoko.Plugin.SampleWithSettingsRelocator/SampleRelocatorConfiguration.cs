using System.ComponentModel.DataAnnotations;
using Shoko.Abstractions.Config;

namespace Shoko.Plugin.SampleWithSettingsRelocator;

/// <summary>
/// The settings for one relocation preset that uses the sample relocator.
/// </summary>
/// <remarks>
/// A relocation provider configuration is not stored on its own. Each preset
/// the user creates carries its own copy, and the server hands the preset's
/// copy to <see cref="SampleRelocator.GetPath"/>. The property initializers are
/// the defaults for a new preset.
/// </remarks>
public class SampleRelocatorConfiguration : IRelocationProviderConfiguration
{
    /// <summary>
    /// Put the prefix in front of every file name.
    /// </summary>
    [Display(Name = "Apply Prefix")]
    public bool ApplyPrefix { get; set; } = true;

    /// <summary>
    /// The text put in front of every file name when the prefix is applied.
    /// </summary>
    [Display(Name = "Prefix")]
    public string Prefix { get; set; } = "[Renamed] ";

    /// <summary>
    /// Sort files into a folder per group, and a folder per series inside it.
    /// Files of a series that already has a folder go next to their siblings.
    /// </summary>
    [Display(Name = "Sort Into Folders")]
    public bool SortIntoFolders { get; set; } = true;
}
