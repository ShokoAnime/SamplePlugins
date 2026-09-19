using Shoko.Abstractions.Extensions;
using Shoko.Abstractions.Video.Relocation;

namespace Shoko.Plugin.OriginalNameRenamer;

/// <summary>
/// Renames a file to its original release name, as recorded by the release
/// info provider that matched it.
/// </summary>
/// <remarks>
/// The provider ID is derived from this type's full name, and every preset
/// the user saves points at that ID. Pick the namespace and class name before
/// you ship, and don't change them afterwards.
/// </remarks>
public class OriginalNameRenamer : IRelocationProvider
{
    /// <inheritdoc/>
    public string Name => "Original Name";

    /// <inheritdoc/>
    public string Description => "Renames files to the name they were released under. Never moves them.";

    /// <inheritdoc/>
    /// <remarks>
    /// Only reported to clients. The server never checks it, so
    /// <see cref="GetPath"/> still has to skip the move itself.
    /// </remarks>
    public bool SupportsMoving => false;

    /// <inheritdoc/>
    public RelocationResult GetPath(RelocationContext context)
    {
        // GetPath runs for previews too, and the context looks identical, so
        // it must never have side effects. Return an error rather than
        // throwing: the message is what the user sees.
        var result = new RelocationResult { SkipMove = true };
        if (!context.RenameEnabled)
        {
            result.SkipRename = true;
            return result;
        }

        if (context.Video.ReleaseInfo?.OriginalFilename is not { Length: > 0 } originalFilename)
            return RelocationResult.FromError("The release has no original file name.");

        // The name comes from a remote database, so make it safe for every
        // file system before handing it back.
        result.FileName = originalFilename.ReplaceInvalidPathCharacters();
        return result;
    }
}
