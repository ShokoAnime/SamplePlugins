using System;
using Microsoft.Extensions.Logging;
using Shoko.Plugin.Abstractions;
using Shoko.Plugin.Abstractions.Attributes;
using Shoko.Plugin.Abstractions.Events;

namespace Shoko.Plugin.OriginalNameRenamer;

// It's better to use an Attribute here, just in case you rename the class. This ID is what is used to identify the plugin in the store configs
[RenamerID("OriginalNameRenamer")]
public class OriginalNameRenamer : IRenamer
{
    // Use Microsoft.Extensions.Logging. The Dependency Injection container will inject the logger.
    private readonly ILogger<OriginalNameRenamer> _logger;

    // This is used for a Name in the webui
    // Gets the current filename of the DLL (simplified)
    // Resolves to "Shoko.Plugin.OriginalNameRenamer"
    // Another option is to use GetType().Name to get the name of this class
    public string Name => GetType().Assembly.GetName().Name;
    // this is used for a description in the webui
    public string Description => "Renames files to the name that AniDB has listed at the time of release";
    public bool SupportsMoving => false;
    public bool SupportsRenaming => true;

    public OriginalNameRenamer(ILogger<OriginalNameRenamer> logger)
    {
        _logger = logger;
    }

    public RelocationResult GetNewPath(RelocationEventArgs args)
    {
        try
        {
            // This doesn't do much. It checks if there's an AniDB File, and returns the original filename
            var originalFilename = args.File.Video?.AniDB?.OriginalFilename;
            if (string.IsNullOrEmpty(originalFilename))
                return new RelocationResult {Error = new RelocationError("No Original Filename was found")};

            // this doesn't support moving, so we just return the original filename
            return new RelocationResult { FileName = originalFilename };
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