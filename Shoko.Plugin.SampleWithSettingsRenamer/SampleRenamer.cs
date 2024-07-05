using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Shoko.Plugin.Abstractions;
using Shoko.Plugin.Abstractions.Attributes;
using Shoko.Plugin.Abstractions.DataModels;

namespace Shoko.Plugin.SampleWithSettingsRenamer;

[RenamerID("SampleWithSettingsRenamer")]
public class SampleRenamer : IRenamer<SampleSettings>
{
    // Use Microsoft.Extensions.Logging. The Dependency Injection container will inject the logger.
    private readonly ILogger<SampleRenamer> _logger;

    // This is used for a Name in the webui
    // Gets the current filename of the DLL (simplified)
    // Resolves to "Shoko.Plugin.OriginalNameRenamer"
    // Another option is to use GetType().Name to get the name of this class
    public string Name => GetType().Assembly.GetName().Name;

    // this is used for a description in the webui
    public string Description => "A sample plugin that renames to a simple unified format and moves to a grouped folder structure";
    public bool SupportsMoving => false;
    public bool SupportsRenaming => true;

    public SampleSettings DefaultSettings => new()
    {
        ApplyPrefix = true,
        Prefix = "[Renamed from my plugin] "
    };

    public SampleRenamer(ILogger<SampleRenamer> logger)
    {
        _logger = logger;
    }

    public RelocationResult GetNewPath(RelocationEventArgs<SampleSettings> args)
    {
        try
        {
            // The question marks everywhere are called Null Coalescence. It's a shorthand for checking if things exist.

            // Technically, there can be more than one episode, series, and group (https://anidb.net/episode/129141).
            // almost always, there will be only one.

            // the settings are in event args
            var settings = args.Settings;

            // get the release group
            var release = args.FileInfo.VideoInfo?.AniDB?.ReleaseGroup.Name;
            _logger.LogInformation($"Release Group: {release}");

            // get the anime info
            var animeInfo = args.AnimeInfo.FirstOrDefault();

            // get the main romaji title
            var animeName = animeInfo?.Titles
                .FirstOrDefault(a => a.Language == TitleLanguage.Romaji && a.Type == TitleType.Main)?.Title;

            // Filenames must be consistent (because OCD), so cancel and return if we can't make a consistent filename style
            if (string.IsNullOrEmpty(animeName))
            {
                return new RelocationResult
                {
                    Error = new MoveRenameError("No Anime Name was found")
                };
            }

            _logger.LogInformation($"AnimeName: {animeName}");

            // Get the episode info
            var episodeInfo = args.EpisodeInfo.FirstOrDefault();

            if (episodeInfo == null)
            {
                return new RelocationResult
                {
                    Error = new MoveRenameError("No Episode Info was found")
                };
            }

            string paddedEpisodeNumber = null;
            switch (episodeInfo.Type)
            {
                case EpisodeType.Episode:
                    paddedEpisodeNumber = episodeInfo.EpisodeNumber.PadZeroes(animeInfo.EpisodeCounts.Episodes);
                    break;
                case EpisodeType.Credits:
                    paddedEpisodeNumber = "C" + episodeInfo.EpisodeNumber.PadZeroes(animeInfo.EpisodeCounts.Credits);
                    break;
                case EpisodeType.Special:
                    paddedEpisodeNumber = "S" + episodeInfo.EpisodeNumber.PadZeroes(animeInfo.EpisodeCounts.Specials);
                    break;
                case EpisodeType.Trailer:
                    paddedEpisodeNumber = "T" + episodeInfo.EpisodeNumber.PadZeroes(animeInfo.EpisodeCounts.Trailers);
                    break;
                case EpisodeType.Parody:
                    paddedEpisodeNumber = "P" + episodeInfo.EpisodeNumber.PadZeroes(animeInfo.EpisodeCounts.Parodies);
                    break;
                case EpisodeType.Other:
                    paddedEpisodeNumber = "O" + episodeInfo.EpisodeNumber.PadZeroes(animeInfo.EpisodeCounts.Others);
                    break;
            }

            _logger.LogInformation($"Padded Episode Number: {paddedEpisodeNumber}");

            // get the info about the video stream from the MediaInfo
            var videoInfo = args.FileInfo.VideoInfo?.MediaInfo?.Video;

            if (videoInfo == null)
            {
                return new RelocationResult
                {
                    Error = new MoveRenameError("No Video Info was found")
                };
            }

            // Get the extension of the original filename, it includes the .
            var ext = Path.GetExtension(args.FileInfo.FileName);

            // The $ allows building a string with the squiggle brackets
            // build a string like "[HorribleSubs] Boku no Hero Academia - 04 [720p HEVC].mkv"
            var result =
                $"[{release}] {animeName} - {paddedEpisodeNumber} [{videoInfo.StandardizedResolution} {videoInfo.SimplifiedCodec}]{ext}";

            // Use the Setting ApplyPrefix and Prefix to determine if we should apply a prefix
            if (settings.ApplyPrefix && !string.IsNullOrEmpty(settings.Prefix)) result = settings.Prefix + result;
            result = result.ReplaceInvalidPathCharacters();

            // now for the destination
            // Note: ReplaceInvalidPathCharacters() replaces things like slashes, pluses, etc. with Unicode that looks similar

            // Get the first available import folder that is a drop destination
            var destinationImportFolder =
                args.AvailableFolders.First(a => a.DropFolderType.HasFlag(DropFolderType.Destination));

            // Get a group name.
            var groupName = args.GroupInfo.First().Name.ReplaceInvalidPathCharacters();
            _logger.LogInformation($"GroupName: {groupName}");

            // There are very few cases where no x-jat main (romaji) title is available, but it happens.
            var seriesNameWithFallback =
                (args.AnimeInfo.First().Titles
                     .FirstOrDefault(a => a.Language == TitleLanguage.Romaji && a.Type == TitleType.Main)?.Title ??
                 args.AnimeInfo.First().Titles.First().Title).ReplaceInvalidPathCharacters();

            _logger.LogInformation($"SeriesName: {seriesNameWithFallback}");

            // Use Path.Combine to form subdirectories with the slashes and whatnot handled for you.
            var destinationPath = Path.Combine(groupName, seriesNameWithFallback);

            // Set the result
            return new RelocationResult
            {
                FileName = result,
                DestinationImportFolder = destinationImportFolder,
                Path = destinationPath
            };
        }
        catch (Exception e)
        {
            // Log the error. We like to know when stuff breaks.
            _logger.LogError(e, $"Unable to get new filename for {args.FileInfo.FileName}");
            return new RelocationResult
            {
                Error = new MoveRenameError($"Unable to get new filename for {args.FileInfo.FileName}", e)
            };
        }
    }
}