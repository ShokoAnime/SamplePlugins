using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shoko.Abstractions.Metadata.Events;
using Shoko.Abstractions.Metadata.Services;
using Shoko.Abstractions.Video.Events;
using Shoko.Abstractions.Video.Services;
using Shoko.QueueProcessor.Abstractions;

namespace Shoko.Plugin.SampleEvents;

/// <summary>
/// Listens to video and metadata events for as long as the server runs.
/// </summary>
/// <remarks>
/// Every handler here follows the same three rules. It is thread-safe,
/// because events are raised from worker threads, several at once. It is
/// idempotent, because the same event can arrive more than once. And it
/// catches its own exceptions, because it runs inside the code that raised
/// the event, and a throw there breaks the server's work rather than yours.
/// Anything slower than a log line goes to the queue.
/// </remarks>
/// <param name="videoService">The source of the file events.</param>
/// <param name="releaseService">The source of the release events.</param>
/// <param name="metadataService">The source of the series events.</param>
/// <param name="queueScheduler">The queue the work is handed to.</param>
/// <param name="logger">The logger.</param>
public sealed class LibraryEventListener(
    IVideoService videoService,
    IVideoReleaseService releaseService,
    IMetadataService metadataService,
    IQueueScheduler queueScheduler,
    ILogger<LibraryEventListener> logger
) : IHostedService
{
    // Paths detected but not yet hashed. VideoFileDetected is raised again on
    // every scan until the file is hashed, from several threads at once, so
    // this remembers what was already reported.
    private readonly ConcurrentDictionary<string, byte> _pendingPaths = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        videoService.VideoFileDetected += OnVideoFileDetected;
        videoService.VideoFileHashed += OnVideoFileHashed;
        releaseService.ReleaseSaved += OnReleaseSaved;
        metadataService.SeriesUpdated += OnSeriesUpdated;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        // The services outlive this listener, so leave nothing attached.
        videoService.VideoFileDetected -= OnVideoFileDetected;
        videoService.VideoFileHashed -= OnVideoFileHashed;
        releaseService.ReleaseSaved -= OnReleaseSaved;
        metadataService.SeriesUpdated -= OnSeriesUpdated;
        return Task.CompletedTask;
    }

    #region Handlers

    private void OnVideoFileDetected(object? sender, VideoFileDetectedEventArgs eventArgs)
    {
        try
        {
            if (_pendingPaths.TryAdd(eventArgs.Path, 0))
                logger.LogInformation("New file detected: {Path}", eventArgs.Path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle a detected file.");
        }
    }

    private void OnVideoFileHashed(object? sender, VideoFileHashedEventArgs eventArgs)
    {
        try
        {
            _pendingPaths.TryRemove(eventArgs.Path, out _);
            if (eventArgs.IsNewVideo)
                logger.LogInformation("New video {VideoID} hashed: {Path}", eventArgs.Video.LocalID, eventArgs.Path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle a hashed file.");
        }
    }

    private void OnReleaseSaved(object? sender, VideoReleaseSavedEventArgs eventArgs)
    {
        // The release links the video to its series, so this is the first
        // point at which there is a series to summarize. ID is the series'
        // MetadataGuid; the Shoko lookups take its LocalID.
        try
        {
            foreach (var series in eventArgs.Video.Series)
                _ = ScheduleSummary(series.LocalID);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle a saved release for video {VideoID}.", eventArgs.Video.LocalID);
        }
    }

    private void OnSeriesUpdated(object? sender, SeriesInfoUpdatedEventArgs eventArgs)
    {
        // Raised for metadata from every provider, not only for Shoko's own
        // series, so follow the link back to the Shoko series it belongs to.
        try
        {
            foreach (var seriesID in eventArgs.SeriesInfo.ShokoSeriesIDs)
                _ = ScheduleSummary(seriesID);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle an updated series.");
        }
    }

    #endregion

    #region Helpers

    private async Task ScheduleSummary(int seriesID)
    {
        // Enqueueing the same job for the same series while one is already
        // waiting is a no-op, which is what makes a burst of events cheap.
        // Nobody awaits this task, so it must not let an exception escape.
        try
        {
            await queueScheduler.Enqueue<SummarizeSeriesJob>(job => job.SeriesID = seriesID).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to schedule a summary for series {SeriesID}.", seriesID);
        }
    }

    #endregion
}
