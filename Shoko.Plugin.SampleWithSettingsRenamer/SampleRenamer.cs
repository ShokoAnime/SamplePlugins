using System.IO;
using System.Linq;
using Shoko.Abstractions.Extensions;
using Shoko.Abstractions.Metadata.Enums;
using Shoko.Abstractions.Metadata.Shoko;
using Shoko.Abstractions.Video.Relocation;
using Shoko.Abstractions.Video.Services;

namespace Shoko.Plugin.SampleWithSettingsRenamer;

/// <summary>
/// Renames files to <c>[Group] Series - 04 [1080p HEVC].mkv</c> and, when
/// enabled, moves them into <c>Group/Series</c> folders.
/// </summary>
/// <remarks>
/// Nothing registers this class. The server finds it, builds it with
/// constructor injection, and keeps that one instance for the life of the
/// process, which is how it gets the relocation service below.
/// </remarks>
/// <param name="relocationService">Used for its destination folder helpers.</param>
public class SampleRenamer(IVideoRelocationService relocationService) : IRelocationProvider<SampleRenamerConfiguration>
{
    /// <inheritdoc/>
    public string Name => "Sample Renamer";

    /// <inheritdoc/>
    public string Description => "Renames files to a fixed format and sorts them into group and series folders.";

    /// <inheritdoc/>
    /// <remarks>
    /// This is the overload the server calls for a provider with a
    /// configuration. The non-generic one is never called, so it is left at
    /// its default.
    /// </remarks>
    public RelocationResult GetPath(RelocationContext<SampleRenamerConfiguration> context)
    {
        var configuration = context.Configuration;

        // SupportsUnrecognized and SupportsIncompleteMetadata are left false,
        // so every file that gets here has at least one episode and series.
        // Checking anyway keeps a mistake in the server from becoming a crash.
        if (context.Episodes is not [var episode, ..] || context.Series is not [var series, ..])
            return RelocationResult.FromError("The file is not linked to an episode and series.");

        var result = new RelocationResult();
        var seriesName = GetRomajiTitle(series).ReplaceInvalidPathCharacters();

        #region Rename

        if (context.RenameEnabled)
        {
            if (context.Video.MediaInfo?.VideoStream is not { } videoStream)
                return RelocationResult.FromError("The file has no media info, so its resolution and codec are unknown.");

            var release = context.Video.ReleaseInfo?.Group is { ShortName: { Length: > 0 } groupName } ? $"[{groupName}] " : string.Empty;
            var episodeNumber = GetEpisodeNumber(episode, series);
            var extension = Path.GetExtension(context.File.FileName);
            var fileName = $"{release}{seriesName} - {episodeNumber} [{videoStream.Resolution} {videoStream.Codec.Simplified}]{extension}";
            if (configuration.ApplyPrefix && !string.IsNullOrEmpty(configuration.Prefix))
                fileName = configuration.Prefix + fileName;

            result.FileName = fileName.ReplaceInvalidPathCharacters();
        }
        else
        {
            result.SkipRename = true;
        }

        #endregion

        #region Move

        if (context.MoveEnabled && configuration.SortIntoFolders)
        {
            if (relocationService.GetExistingSeriesLocationWithSpace(context) is { } existing)
            {
                result.ManagedFolder = existing.ManagedFolder;
                result.Path = existing.RelativePath;
            }
            else if (relocationService.GetFirstDestinationWithSpace(context) is { } destination)
            {
                var groupName = context.Groups is [var group, ..] ? group.Title.ReplaceInvalidPathCharacters() : seriesName;
                result.ManagedFolder = destination;
                result.Path = Path.Combine(groupName, seriesName);
            }
            else
            {
                return RelocationResult.FromError("No destination folder has enough free space.");
            }
        }
        else
        {
            result.SkipMove = true;
        }

        #endregion

        return result;
    }

    #region Helpers

    private static string GetRomajiTitle(IShokoSeries series)
        => series.Titles.FirstOrDefault(title => title is { Language: TitleLanguage.Romaji, Type: TitleType.Main })?.Value ?? series.Title;

    private static string GetEpisodeNumber(IShokoEpisode episode, IShokoSeries series)
    {
        // Pad to the width of the highest number of that type, so the files
        // sort in order: 01 to 12, or 001 to 120.
        var number = episode.EpisodeNumber.PadZeroes(series.EpisodeCounts[episode.Type]);
        return episode.Type switch
        {
            EpisodeType.Episode => number,
            EpisodeType.Special => $"S{number}",
            EpisodeType.Credits => $"C{number}",
            EpisodeType.Trailer => $"T{number}",
            EpisodeType.Parody => $"P{number}",
            _ => $"O{number}",
        };
    }

    #endregion
}
