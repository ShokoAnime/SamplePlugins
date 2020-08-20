using Shoko.Plugin.Abstractions;

namespace Shoko.Plugin.SampleWithSettingsRenamer
{
    public class SampleSettings : IPluginSettings
    {
        public string Prefix { get; set; } = "";
        public bool ApplyPrefix { get; set; } = true;
    }
}