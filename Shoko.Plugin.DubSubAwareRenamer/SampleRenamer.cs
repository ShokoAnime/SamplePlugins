﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Shoko.Plugin.Abstractions;
using Shoko.Plugin.Abstractions.DataModels;
using Shoko.Plugin.Abstractions.Events;

namespace Renamer.Baine;

/// <summary>
/// Baines custom Renamer
/// Target Folder Structure is based on available dub/sub languages, as well as being restricted to being >18+
/// </summary>
public class MyRenamer : IRenamer
{
    //set the name of the plugin. this will show up in settings-server.json
    public string Name => "BaineRenamer";
    public string Description =>
        "Target Folder Structure is based on available dub/sub languages, as well being restricted to being >18+";
    public bool SupportsMoving => true;
    public bool SupportsRenaming => true;

    public RelocationResult GetNewPath(RelocationEventArgs args)
    {
        var filename = GetFilename(args);
        if (string.IsNullOrEmpty(filename)) return new RelocationResult {Error = new RelocationError("Filename is empty")};
        var destination = GetDestination(args);
        if (destination == default) return new RelocationResult {Error = new RelocationError("Destination is empty")};

        return new RelocationResult
        {
            FileName = filename,
            Path = destination.subfolder,
            DestinationImportFolder = destination.destination
        };
    }

    /// <summary>
    /// Get the new filename for a specified file
    /// </summary>
    /// <param name="args">Renaming Arguments, e.g. available folders</param>
    public string GetFilename(RelocationEventArgs args)
    {
        //make args.File easier accessible. this refers to the actual file
        var video = args.File;

        //make the episode in question easier accessible. this refers to the episode the file is linked to
        var episode = args.Episodes.First();

        //make the anime the episode belongs to easier accessible.
        var anime = args.Series.First();

        //start an empty StringBuilder
        //will be used to store the new filename
        var name = new StringBuilder();

        //add the Anime title as defined by preference
        name.Append(GetTitleByPref(anime, TitleType.Official, TitleLanguage.German, TitleLanguage.English,
            TitleLanguage.Romaji));
        //after this: name = Showname

        //only add prefixes and episode numbers when dealing with non-Movie files/episodes
        if (anime.Type != AnimeType.Movie)
        {
            //store the epsiode number as string. will be padded, determined by how many
            //episodes of the same type exist
            string paddedEpisodeNumber = null;

            //perform action based on the episode type
            //adding prefixes to the episode number for Credits, Specials, Trailers, Parodies ond episodes defined as Other
            switch (episode.Type)
            {
                case EpisodeType.Episode:
                    paddedEpisodeNumber = episode.EpisodeNumber.PadZeroes(anime.EpisodeCounts.Episodes);
                    break;
                case EpisodeType.Credits:
                    paddedEpisodeNumber = "C" + episode.EpisodeNumber.PadZeroes(anime.EpisodeCounts.Credits);
                    break;
                case EpisodeType.Special:
                    paddedEpisodeNumber = "S" + episode.EpisodeNumber.PadZeroes(anime.EpisodeCounts.Specials);
                    break;
                case EpisodeType.Trailer:
                    paddedEpisodeNumber = "T" + episode.EpisodeNumber.PadZeroes(anime.EpisodeCounts.Trailers);
                    break;
                case EpisodeType.Parody:
                    paddedEpisodeNumber = "P" + episode.EpisodeNumber.PadZeroes(anime.EpisodeCounts.Parodies);
                    break;
                case EpisodeType.Other:
                    paddedEpisodeNumber = "O" + episode.EpisodeNumber.PadZeroes(anime.EpisodeCounts.Others);
                    break;
            }

            //actually append the padded episode number, storing prefix as well
            name.Append($" - {paddedEpisodeNumber}");
            //after this: name = Showname - S03
        }

        //get the preferred episode name and add it to the name
        name.Append(
            $" - {GetEpNameByPref(episode, TitleLanguage.German, TitleLanguage.English, TitleLanguage.Romaji)}");
        //after this: name = Showname - S03 - SpecialName

        //get and append the files extension
        name.Append($"{Path.GetExtension(video.FileName)}");
        //after this: name = Showname - S03 - Specialname.mkv

        //set the name as the result, replacing invalid path characters (e.g. '/') with similar looking Unicode Characters
        return name.ToString().ReplaceInvalidPathCharacters();
    }

    /// <summary>
    /// Get the new path for a specified file.
    /// The target path depends on age restriction and available dubs/subs
    /// </summary>
    /// <param name="args">Arguments for the process, contains File and more</param>
    public (IImportFolder destination, string subfolder) GetDestination(RelocationEventArgs args)
    {
        //get the anime the file in question is linked to
        var anime = args.Series.First();

        //get the File of the file in question
        var video = args.File.Video;

        //check if the anime in question is restricted to 18+
        var isPorn = anime.Restricted;

        //instantiate lists for various stream information
        //these include Dub/Sub-Languages as readable by mediainfo
        //as well as Dub/Sub-Languages that AniDB provides for the file, if it is known
        IReadOnlyList<ITextStream> textStreamsFile = null;
        IReadOnlyList<IAudioStream> audioStreamsFile = null;
        IReadOnlyList<TitleLanguage> textLanguagesAniDB = null;
        IReadOnlyList<TitleLanguage> audioLanguagesAniDB = null;

        try
        {
            //sub streams as provided by mediainfo
            textStreamsFile = video?.MediaInfo?.TextStreams;

            //dub streams as provided by mediainfo
            audioStreamsFile = video?.MediaInfo?.AudioStreams;

            //sub languages as provided by anidb
            textLanguagesAniDB = video?.AniDB?.MediaInfo.SubLanguages;

            //sub languages as provided by anidb
            audioLanguagesAniDB = video?.AniDB?.MediaInfo.AudioLanguages;
        }
        catch
        {
            // ignored
        }

        //define various bools
        //those will only get set to true if the respective stream in the relevant language is found
        var isEngDub = false;
        var isEngSub = false;
        var isGerDub = false;
        var isGerSub = false;

        //check if mediainfo provides us with audiostreams. if so, check if the language of any of them matches the desired one.
        //check if anidb provides us with information about the audiostreams. if so, check if the language of any of them matches the desired one.
        //if any of the above is true, set the respective bool to true
        //the same process applies to both dub and sub
        if ((audioStreamsFile != null && audioStreamsFile!.Any(a => a!.LanguageCode?.ToLower() == "ger")) ||
            (audioLanguagesAniDB != null && audioLanguagesAniDB!.Any(a => a == TitleLanguage.German)))
            isGerDub = true;

        if ((textStreamsFile != null && textStreamsFile!.Any(t => t!.LanguageCode?.ToLower() == "ger")) ||
            (textLanguagesAniDB != null && textLanguagesAniDB!.Any(t => t == TitleLanguage.German)))
            isGerSub = true;

        if ((audioStreamsFile != null && audioStreamsFile!.Any(a => a!.LanguageCode?.ToLower() == "eng")) ||
            (audioLanguagesAniDB != null && audioLanguagesAniDB!.Any(a => a == TitleLanguage.English)))
            isEngDub = true;

        if ((textStreamsFile != null && textStreamsFile!.Any(t => t!.LanguageCode?.ToLower() == "eng")) ||
            (textLanguagesAniDB != null && textLanguagesAniDB!.Any(t => t == TitleLanguage.English)))
            isEngSub = true;

        //define location based on the OS shokoserver is currently running on
        var location = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/mnt/array/" : "Z:\\";

        //define the first subfolder depending on age restriction
        location += isPorn ? "Hentai" : "Anime";

        //add a directory separator char. this automatically switches between the proper char for the current OS
        location += Path.DirectorySeparatorChar;

        //a while true loop. be carefull here, since this would always require a default break; otherwise this ever ends
        //used to evaluate the previously set bools and add the 2nd subfolder depending on available dubs/subs
        //if no choice can be made, a fallback folder is used for manual processing
        while (true)
        {
            if (isGerDub)
            {
                location += "GerDub";
                break;
            }

            if (isGerSub)
            {
                location += "GerSub";
                break;
            }

            if ((isEngDub || isEngSub))
            {
                location += "Other";
                break;
            }

            location += "_manual";
            break;
        }

        // check if any of the available folders matches the constructed path in location, set it as destination
        var dest = args.AvailableFolders.FirstOrDefault(a => a.Path.TrimEnd('/') == location);

        // DestinationPath is the name of the final subfolder containing the episode files. Get it by preference
        var destinationPath =
            GetTitleByPref(anime, TitleType.Official, TitleLanguage.German, TitleLanguage.English,
                TitleLanguage.Romaji).ReplaceInvalidPathCharacters();

        return (dest, destinationPath);
    }

    /// <summary>
    /// Get anime title as specified by preference. Order matters. if nothing found, preferred title is returned
    /// </summary>
    /// <param name="anime">IAnime object representing the Anime the title has to be searched for</param>
    /// <param name="type">TitleType, eg. TitleType.Official or TitleType.Short </param>
    /// <param name="langs">Arguments Array taking in the TitleLanguages that should be search for.</param>
    /// <returns>string representing the Anime Title for the first language a title is found for</returns>
    private string GetTitleByPref(ISeries anime, TitleType type, params TitleLanguage[] langs)
    {
        //get all titles
        var titles = (List<AnimeTitle>)anime.Titles;

        //iterate over the given TitleLanguages in langs
        foreach (var lang in langs)
        {
            //set title to the first found title of the defined language. if nothing found title will stay null
            var title = titles.FirstOrDefault(s => s.Language == lang && s.Type == type)?.Title;

            //if title is found, aka title not null, return it
            if (title != null) return title;
        }

        //no title found for the preferred languages, return the preferred title as defined by shoko
        return anime.PreferredTitle;
    }

    /// <summary>
    /// Get Episode Name/Title as specified by preference. if nothing found, the first available is returned
    /// </summary>
    /// <param name="episode">IEpisode object representing the episode to search the name for</param>
    /// <param name="langs">Arguments array taking in the TitleLanguages that should be search for.</param>
    /// <returns>string representing the episode name for the first language a name is found for</returns>
    private string GetEpNameByPref(IEpisode episode, params TitleLanguage[] langs)
    {
        //iterate over all passed TitleLanguages
        foreach (var lang in langs)
        {
            //set the title to the first found title whose language matches with the search one.
            //if none is found, title is null
            var title = episode.Titles.FirstOrDefault(s => s.Language == lang)?.Title;

            //return the found title if title is not null
            if (title != null) return title;
        }

        //no title for any given TitleLanguage found, return the first available.
        return episode.Titles.First().Title;
    }
}