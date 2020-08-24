using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NLog;
using Shoko.Plugin.Abstractions;
using Shoko.Plugin.Abstractions.DataModels;

namespace Shoko.Plugin.SampleWithSettingsRenamer
{
    public class OriginalNameRenamer : IRenamer
    {
        // Be careful when using Nuget (NLog had to be installed for this project).
        // Shoko already has and configures NLog, so it's safe to use, but other things may not be
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Gets the current filename of the DLL (simplified)
        // Resolves to "Shoko.Plugin.OriginalNameRenamer"
        public string Name => Assembly.GetExecutingAssembly().GetName().Name;

        public void Load() { }

        public void OnSettingsLoaded(IPluginSettings settings) { }

        public void GetFilename(RenameEventArgs args)
        {
            try
            {
                // This renamer doesn't do much. It check if there's an AniDB File, and returns the original filename
                string originalFilename = args.FileInfo?.AniDBFileInfo?.OriginalFilename;
                if (string.IsNullOrEmpty(originalFilename)) return;

                // Set the result
                args.Result = originalFilename;
            }
            catch (Exception e)
            {
                // Log the error. We like to know when stuff breaks.
                Logger.Error(e, $"Unable to get new filename for {args.FileInfo?.Filename}");
            }
        }

        public void GetDestination(MoveEventArgs args)
        {
            // defer to next plugin or legacy drop folders
        }
    }
}