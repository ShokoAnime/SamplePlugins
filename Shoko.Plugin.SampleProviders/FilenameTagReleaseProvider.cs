using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Shoko.Abstractions.Extensions;
using Shoko.Abstractions.Video.Release;

namespace Shoko.Plugin.SampleProviders;

/// <summary>
/// Matches a file to the AniDB episodes named in its file name, written as
/// <c>[anidb-episode-12345]</c>. A file covering two episodes carries two tags.
/// </summary>
/// <remarks>
/// A new release info provider starts disabled. It is listed on the release
/// provider settings, but asked about no file until a user enables it there.
/// Its ID is derived from this type's full name, and the user's enabled flag
/// and priority are stored against that ID, so pick the namespace and class
/// name before you ship.
/// </remarks>
public partial class FilenameTagReleaseProvider : IReleaseInfoProvider
{
    private const string IdPrefix = "filename-tag://";

    /// <inheritdoc/>
    public string Name => "Filename Tag";

    /// <inheritdoc/>
    public string Description => "Matches files whose names carry AniDB episode tags, such as \"[anidb-episode-12345]\".";

    /// <inheritdoc/>
    public Task<ReleaseInfo?> GetReleaseInfoForVideo(ReleaseInfoContext context, CancellationToken cancellationToken)
    {
        // IsAutomatic tells an import apart from a user asking for a search.
        // A provider that guesses might only guess when a user asked; a tag
        // the user wrote is trusted either way.
        var (video, _) = context;
        var fileNames = video.Files.Select(file => file.FileName).Append(video.EarliestKnownName);
        foreach (var fileName in fileNames)
        {
            if (string.IsNullOrEmpty(fileName))
                continue;

            var episodeIDs = TagRegex().Matches(Path.GetFileNameWithoutExtension(fileName))
                .Select(match => int.Parse(match.Groups["episodeID"].ValueSpan))
                .Distinct()
                .ToList();
            if (episodeIDs.Count > 0)
                return Task.FromResult<ReleaseInfo?>(CreateReleaseInfo(episodeIDs, fileName));
        }

        // No match is null, not an exception. A throw ends the whole search,
        // and the providers after this one are never asked.
        return Task.FromResult<ReleaseInfo?>(null);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The ID this provider hands out already holds the episode IDs, so a
    /// release can be rebuilt from it without looking anything up.
    /// </remarks>
    public Task<ReleaseInfo?> GetReleaseInfoById(string releaseId, CancellationToken cancellationToken)
    {
        if (!releaseId.StartsWith(IdPrefix, StringComparison.Ordinal))
            return Task.FromResult<ReleaseInfo?>(null);

        var episodeIDs = new List<int>();
        foreach (var part in releaseId[IdPrefix.Length..].Split(','))
        {
            if (!int.TryParse(part, out var episodeID) || episodeID <= 0)
                return Task.FromResult<ReleaseInfo?>(null);

            episodeIDs.Add(episodeID);
        }

        return Task.FromResult<ReleaseInfo?>(episodeIDs.Count > 0 ? CreateReleaseInfo(episodeIDs, null) : null);
    }

    #region Helpers

    private static ReleaseInfo CreateReleaseInfo(IReadOnlyList<int> episodeIDs, string? originalFilename)
        => new()
        {
            ID = IdPrefix + string.Join(',', episodeIDs),
            OriginalFilename = originalFilename,
            // Only the episode ID is known. The server fills in the anime ID
            // from it, and sets the provider name itself.
            CrossReferences = episodeIDs.Select(episodeID => ReleaseVideoCrossReference.ForAniDB(episodeID)).ToList(),
        };

    [GeneratedRegex(@"\[anidb-episode-(?<episodeID>[1-9][0-9]{0,8})\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TagRegex();

    #endregion
}
