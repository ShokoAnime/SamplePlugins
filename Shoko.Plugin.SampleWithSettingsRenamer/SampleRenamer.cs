using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NLog;
using Shoko.Plugin.Abstractions;
using Shoko.Plugin.Abstractions.Attributes;
using Shoko.Plugin.Abstractions.DataModels;

namespace Shoko.Plugin.SampleWithSettingsRenamer;

// Note!!!! This doesn't work ATM! The settings framework needs work.
[Renamer("SampleWithSettingsRenamer",
    Description = "A sample plugin that renames to a simple unified format and moves to a grouped folder structure")]
public class SampleRenamer : IRenamer
{
    // Be careful when using Nuget (NLog had to be installed for this project).
    // Shoko already has and configures NLog, so it's safe to use, but other things may not be
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static SampleSettings Settings { get; set; }

    private static ISettingsProvider SettingsProvider { get; set; }

    // Gets the current filename of the DLL (simplified)
    // Resolves to "Shoko.Plugin.SampleWithSettingsRenamer"
    public string Name => Assembly.GetExecutingAssembly().GetName().Name;

    public SampleRenamer(ISettingsProvider settingsProvider)
    {
        // save for later
        SettingsProvider = settingsProvider;
    }

    public void Load()
    {
        // ignore. We are a renamer
    }

    public void OnSettingsLoaded(IPluginSettings settings)
    {
        // Save this for later.
        Settings = settings as SampleSettings;
    }

    public string GetFilename(RenameEventArgs args)
    {
        try
        {
            // The question marks everywhere are called Null Coalescence. It's a shorthand for checking if things exist.

            // Technically, there can be more than one episode, series, and group (https://anidb.net/episode/129141).
            // essentially always, there will be only one.

            // get the release group
            string release = args.FileInfo.AniDBFileInfo.ReleaseGroup.Name;
            Logger.Info($"Release Group: {release}");

            // get the anime info
            IAnime animeInfo = args.AnimeInfo.FirstOrDefault();

            // get the romaji title
            string animeName = animeInfo?.Titles
                .FirstOrDefault(a => a.Language == TitleLanguage.Romaji && a.Type == TitleType.Main)?.Title;

            // Filenames must be consistent (because OCD), so cancel and return if we can't make a consistent filename style
            if (string.IsNullOrEmpty(animeName))
            {
                args.Cancel = true;
                return null;
            }

            Logger.Info($"AnimeName: {animeName}");

            // Get the episode info
            IEpisode episodeInfo = args.EpisodeInfo.First();

            string paddedEpisodeNumber = null;
            switch (episodeInfo.Type)
            {
                case EpisodeType.Episode:
                    paddedEpisodeNumber = episodeInfo.Number.PadZeroes(animeInfo.EpisodeCounts.Episodes);
                    break;
                case EpisodeType.Credits:
                    paddedEpisodeNumber = "C" + episodeInfo.Number.PadZeroes(animeInfo.EpisodeCounts.Credits);
                    break;
                case EpisodeType.Special:
                    paddedEpisodeNumber = "S" + episodeInfo.Number.PadZeroes(animeInfo.EpisodeCounts.Specials);
                    break;
                case EpisodeType.Trailer:
                    paddedEpisodeNumber = "T" + episodeInfo.Number.PadZeroes(animeInfo.EpisodeCounts.Trailers);
                    break;
                case EpisodeType.Parody:
                    paddedEpisodeNumber = "P" + episodeInfo.Number.PadZeroes(animeInfo.EpisodeCounts.Parodies);
                    break;
                case EpisodeType.Other:
                    paddedEpisodeNumber = "O" + episodeInfo.Number.PadZeroes(animeInfo.EpisodeCounts.Others);
                    break;
            }

            Logger.Info($"Padded Episode Number: {paddedEpisodeNumber}");

            // get the info about the video stream from the MediaInfo
            IVideoStream videoInfo = args.FileInfo.MediaInfo.Video;

            // Get the extension of the original filename, it includes the .
            string ext = Path.GetExtension(args.FileInfo.Filename);

            // The $ allows building a string with the squiggle brackets
            // build a string like "[HorribleSubs] Boku no Hero Academia - 04 [720p HEVC].mkv"
            string result =
                $"[{release}] {animeName} - {paddedEpisodeNumber} [{videoInfo.StandardizedResolution} {videoInfo.SimplifiedCodec}]{ext}";

            // Use the Setting ApplyPrefix and Prefix to determine if we should apply a prefix
            if (Settings.ApplyPrefix && !string.IsNullOrEmpty(Settings.Prefix)) result = Settings.Prefix + result;
            result = result.ReplaceInvalidPathCharacters();

            // Set the result
            return result;
        }
        catch (Exception e)
        {
            // Clearly the Prefix broke it (it didn't, probably), so we'll disable the Prefix
            Settings.ApplyPrefix = false;
            SettingsProvider.SaveSettings(Settings);

            // Log the error. We like to know when stuff breaks.
            Logger.Error(e, $"Unable to get new filename for {args.FileInfo?.Filename}");
        }

        return null;
    }

    public (IImportFolder destination, string subfolder) GetDestination(MoveEventArgs args)
    {
        try
        {
            // Note: ReplaceInvalidPathCharacters() replaces things like slashes, pluses, etc with Unicode that looks similar

            // Get the first available import folder that is a drop destination
            var destinationImportFolder =
                args.AvailableFolders.First(a => a.DropFolderType.HasFlag(DropFolderType.Destination));

            // Get a group name.
            string groupName = args.GroupInfo.First().Name.ReplaceInvalidPathCharacters();
            Logger.Info($"GroupName: {groupName}");

            // There are very few cases where no x-jat main (romaji) title is available, but it happens.
            string seriesNameWithFallback =
                (args.AnimeInfo.First().Titles
                     .FirstOrDefault(a => a.Language == TitleLanguage.Romaji && a.Type == TitleType.Main)?.Title ??
                 args.AnimeInfo.First().Titles.First().Title).ReplaceInvalidPathCharacters();
            Logger.Info($"SeriesName: {seriesNameWithFallback}");

            // Use Path.Combine to form subdirectories with the slashes and whatnot handled for you.
            var destinationPath = Path.Combine(groupName, seriesNameWithFallback);

            return (destinationImportFolder, destinationPath);
        }
        catch (Exception e)
        {
            // Log the error to Server
            Logger.Error(e, $"Unable to get destination for {args.FileInfo?.Filename}");
            throw;
        }
    }
}