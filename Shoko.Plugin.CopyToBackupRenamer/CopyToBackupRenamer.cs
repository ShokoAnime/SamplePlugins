using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Shoko.Plugin.Abstractions;
using Shoko.Plugin.Abstractions.Attributes;
using Shoko.Plugin.Abstractions.DataModels;

namespace Shoko.Plugin.CopyToBackupRenamer;

[RenamerID("CopyToBackupRenamer")]
public class CopyToBackupRenamer : IRenamer<BackupSettings>
{
    // Use Microsoft.Extensions.Logging. The Dependency Injection container will inject the logger.
    private readonly ILogger<CopyToBackupRenamer> _logger;

    // This is used for a Name in the webui
    // Gets the current filename of the DLL (simplified)
    // Resolves to "Shoko.Plugin.OriginalNameRenamer"
    // Another option is to use GetType().Name to get the name of this class
    public string Name => GetType().Assembly.GetName().Name;

    // this is used for a description in the webui
    public string Description => "Doesn't rename or move! It copies files to a backup folder, with a directory structure";
    // It won't run if both of these are false
    public bool SupportsMoving => true;
    public bool SupportsRenaming => false;
    public BackupSettings DefaultSettings => null;

    public CopyToBackupRenamer(ILogger<CopyToBackupRenamer> logger)
    {
        _logger = logger;
    }

    public RelocationResult GetNewPath(RelocationEventArgs<BackupSettings> args)
    {
        var backupPath = args.Settings.BackupPath;
        if (!Directory.Exists(backupPath))
            return new RelocationResult { Error = new MoveRenameError("Backup path does not exist") };

        // Get a group name.
        var groupName = args.GroupInfo.FirstOrDefault()?.Name.ReplaceInvalidPathCharacters();
        if (string.IsNullOrEmpty(groupName))
            return new RelocationResult { Error = new MoveRenameError("No Group Name was found") };

        _logger.LogInformation($"GroupName: {groupName}");

        // There are very few cases where no x-jat main (romaji) title is available, but it happens.
        var seriesNameWithFallback =
            (args.AnimeInfo.First().Titles
                 .FirstOrDefault(a => a.Language == TitleLanguage.Romaji && a.Type == TitleType.Main)?.Title ??
             args.AnimeInfo.First().Titles.First().Title).ReplaceInvalidPathCharacters();
        _logger.LogInformation($"SeriesName: {seriesNameWithFallback}");

        // Use Path.Combine to form subdirectories with the slashes and whatnot handled for you.
        var destinationPath = Path.Combine(backupPath, groupName, seriesNameWithFallback);

        try
        {
            // there is a lot that can go wrong when copying a file, so we are try/catching it

            // first make all of the necessary directories
            Directory.CreateDirectory(Path.Combine(backupPath, groupName));

            // try to copy the source file to the backup destination
            // args.FileInfo.FilePath is the full absolute path to the file
            // args.FileInfo.FilePath is the current filename
            // If you want the file to be renamed prior to this, then enable the setting Import -> RenameThenMove
            File.Copy(args.FileInfo.Path, Path.Combine(destinationPath, args.FileInfo.FileName));
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Unable to copy \"{args.FileInfo.Path}\" to {Path.Combine(destinationPath, args.FileInfo.FileName)}");
        }

        return new RelocationResult
        {
            DestinationImportFolder = args.FileInfo.ImportFolder,
            Path = Path.GetDirectoryName(args.FileInfo.RelativePath)
        };
    }
}