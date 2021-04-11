using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NLog;
using Shoko.Plugin.Abstractions;
using Shoko.Plugin.Abstractions.Attributes;
using Shoko.Plugin.Abstractions.DataModels;

namespace Shoko.Plugin.CopyToBackupRenamer
{
    [Renamer("CopyToBackupRenamer", Description = "Doesn't rename or move! It finds an Import Folder with 'Backup' in the name and copies to it")]
    public class CopyToBackupRenamer : IRenamer
    {
        // Be careful when using Nuget (NLog had to be installed for this project).
        // Shoko already has and configures NLog, so it's safe to use, but other things may not be
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Gets the current filename of the DLL (simplified)
        // Resolves to "Shoko.Plugin.CopyToBackupRenamer"
        public string Name => Assembly.GetExecutingAssembly().GetName().Name;

        public void Load() { }

        public void OnSettingsLoaded(IPluginSettings settings) { }

        public string GetFilename(RenameEventArgs args)
        {
            // defer! We don't do that here
            return null;
        }

        public (IImportFolder destination, string subfolder) GetDestination(MoveEventArgs args)
        {
            // Get import folders, and find the one we want!
            // if an import folder has a name of Backup or Backup is in the path, use that one
            // for the sake of following our own rules, only choose it if it's a Destination or Both
            var folder = args.AvailableFolders.FirstOrDefault(a =>
                a.DropFolderType.HasFlag(DropFolderType.Destination) &&
                (a.Name.Equals("Backup", StringComparison.InvariantCultureIgnoreCase) ||
                a.Location.IndexOf("Backup", StringComparison.InvariantCultureIgnoreCase) > -1));

            if (folder == null)
            {
                Logger.Error("Unable to get Backup folder.");
                return (null, null);
            }

            // Get a group name.
            string groupName = args.GroupInfo.First().Name.ReplaceInvalidPathCharacters();
            Logger.Info($"GroupName: {groupName}");

            if (string.IsNullOrEmpty(groupName)) return (null, null);

            // There are very few cases where no x-jat main (romaji) title is available, but it happens.
            string seriesNameWithFallback =
                (args.AnimeInfo.First().Titles
                     .FirstOrDefault(a => a.Language == TitleLanguage.Romaji && a.Type == TitleType.Main)?.Title ??
                 args.AnimeInfo.First().Titles.First().Title).ReplaceInvalidPathCharacters();
            Logger.Info($"SeriesName: {seriesNameWithFallback}");
            
            if (string.IsNullOrEmpty(seriesNameWithFallback)) return (null, null);

            // Use Path.Combine to form subdirectories with the slashes and whatnot handled for you.
            var destinationPath = Path.Combine(folder.Location, groupName, seriesNameWithFallback);

            try
            {
                // there is a lot that can go wrong when copying a file, so we are try/catching it
                
                // first make all of the necessary directories
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                
                // try to copy the source file to the backup destination
                // args.FileInfo.FilePath is the full absolute path to the file
                // args.FileInfo.FilePath is the current filename
                // If you want the file to be renamed prior to this, then enable the setting Import -> RenameThenMove
                File.Copy(args.FileInfo.FilePath, Path.Combine(destinationPath, args.FileInfo.Filename));
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Unable to copy \"{args.FileInfo.Filename}\" to Backup");
            }
            
            // defer to next plugin or legacy drop folders
            return (null, null);
        }
    }
}