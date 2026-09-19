using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shoko.Abstractions.Metadata.Services;
using Shoko.QueueProcessor.Abstractions;
using Shoko.QueueProcessor.Acquisition.Attributes;
using Shoko.QueueProcessor.Builder;

namespace Shoko.Plugin.SampleEvents;

/// <summary>
/// Logs a short summary of one series. Stands in for whatever real work a
/// plugin would do in response to an event.
/// </summary>
/// <remarks>
/// A job is built fresh for every run, with constructor injection, and its
/// public settable properties are saved with it and restored before
/// <see cref="Process"/>. Those properties also make up the key the queue
/// deduplicates on. The key starts with the class name, not the full name,
/// so <c>[JobKeyGroup]</c> keeps it apart from another plugin's job with the
/// same name.
/// </remarks>
/// <param name="metadataService">Used to look the series up.</param>
/// <param name="logger">The logger.</param>
[DatabaseRequired]
[JobKeyGroup("SampleEvents")]
public class SummarizeSeriesJob(IMetadataService metadataService, ILogger<SummarizeSeriesJob> logger) : IQueueJob
{
    /// <summary>
    /// The Shoko series to summarize.
    /// </summary>
    public int SeriesID { get; set; }

    /// <inheritdoc/>
    public string TypeName => "Summarize Series";

    /// <inheritdoc/>
    public string Title => "Summarizing a series";

    /// <inheritdoc/>
    public Dictionary<string, object> Details => new() { ["Series ID"] = SeriesID };

    /// <inheritdoc/>
    public Task Process()
    {
        // The series can be gone by the time the job runs. That is not an
        // error worth a retry, so log it and finish.
        if (metadataService.GetShokoSeriesByID(SeriesID) is not { } series)
        {
            logger.LogDebug("Series {SeriesID} no longer exists.", SeriesID);
            return Task.CompletedTask;
        }

        logger.LogInformation(
            "{Title}: {EpisodeCount} episodes, {VideoCount} videos, {MissingCount} missing.",
            series.Title,
            series.Episodes.Count,
            series.Videos.Count,
            series.MissingEpisodeCount
        );
        return Task.CompletedTask;
    }
}
