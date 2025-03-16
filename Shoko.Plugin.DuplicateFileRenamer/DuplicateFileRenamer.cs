using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Shoko.Plugin.Abstractions;
using Shoko.Plugin.Abstractions.Attributes;
using Shoko.Plugin.Abstractions.Events;

namespace Shoko.Plugin.DuplicateFileRenamer;

// It's better to use an Attribute here, just in case you rename the class. This ID is what is used to identify the plugin in the store configs
[RenamerID(nameof(DuplicateFileRenamer))]
public class DuplicateFileRenamer : IRenamer
{
    // Use Microsoft.Extensions.Logging. The Dependency Injection container will inject the logger.
    private readonly ILogger<DuplicateFileRenamer> _logger;

    // This is used for a Name in the webui
    // Gets the current filename of the DLL (simplified)
    // Resolves to "Shoko.Plugin.DuplicateFileRenamer"
    // Another option is to use GetType().Name to get the name of this class, though nameof() is recommended, as it's a compile-time constant
    public string Name => GetType().Assembly.GetName().Name;
    // this is used for a description in the webui
    public string Description => "Moves duplicate files to a Managed Folder named \"Duplicate Files\" or that contains \"Duplicate\" in the base path";
    public bool SupportsMoving => true;
    public bool SupportsRenaming => false;

    public DuplicateFileRenamer(ILogger<DuplicateFileRenamer> logger)
    {
        _logger = logger;
    }

    public RelocationResult GetNewPath(RelocationEventArgs args)
    {
        try
        {
            // get the managed folder for the duplicate files
            var folder = args.AvailableFolders.FirstOrDefault(a => a.Name.Equals("Duplicate Files") || a.Path.Contains("Duplicate"));
            if (folder == null) return new RelocationResult { Error = new RelocationError("Unable to get Managed Folder for Duplicate Files") };

            // determine if the file is a duplicate, which is done by checking if there are any other files in any of the episodes whose hashes are the same
            var isDuplicate = args.Episodes.Any(a => a.CrossReferences.Any(v => string.Equals(v.Video?.Hashes.ED2K, args.File.Video?.Hashes.ED2K)));
            if (!isDuplicate) return new RelocationResult { Error = new RelocationError("File is not a duplicate") };

            return new RelocationResult
            {
                DestinationImportFolder = folder,
                Path = args.Series.FirstOrDefault()?.PreferredTitle
            };
        }
        catch (Exception e)
        {
            // Log the error. We like to know when stuff breaks.
            _logger.LogError(e, $"Unable to get new filename for {args.File.FileName}");
            return new RelocationResult
            {
                Error = new RelocationError($"Unable to get new filename for {args.File.FileName}", e)
            };
        }
    }
}