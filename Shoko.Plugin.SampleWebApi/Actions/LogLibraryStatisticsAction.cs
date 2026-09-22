using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shoko.Abstractions.Actions;
using Shoko.Abstractions.Metadata.Services;
using Shoko.Abstractions.Video.Services;

namespace Shoko.Plugin.SampleWebApi.Actions;

/// <summary>
/// A global action: it needs no series, episode or file to run on.
/// </summary>
/// <remarks>
/// Found by the server and registered as transient, so a fresh instance is
/// built for every validation and every run. Keep state in an injected
/// singleton, not here. The action ID is derived from this type's full name,
/// so renaming or moving the class breaks anything holding the old ID.
/// </remarks>
/// <param name="metadataService">Used to count the series.</param>
/// <param name="videoService">Used to count the videos.</param>
/// <param name="logger">The logger.</param>
public class LogLibraryStatisticsAction(
    IMetadataService metadataService,
    IVideoService videoService,
    ILogger<LogLibraryStatisticsAction> logger
) : IExecutableAction
{
    /// <inheritdoc/>
    public string Name => "Log Library Statistics";

    /// <inheritdoc/>
    public string Description => "Writes the number of series and videos to the server log.";

    /// <inheritdoc/>
    /// <remarks>Groups the plugin's actions together under its own name.</remarks>
    public ActionCategory Category => ActionCategory.PluginInferred;

    /// <inheritdoc/>
    /// <remarks>
    /// Must be declared on the action class itself. Leaving it to a base class
    /// fails the server's start-up.
    /// </remarks>
    public ActionPermission Permission => ActionPermission.Admin;

    /// <inheritdoc/>
    public Task Execute(CancellationToken token = default)
    {
        var seriesCount = metadataService.GetAllShokoSeries().Count();
        var videoCount = videoService.GetAllVideos().Count();

        token.ThrowIfCancellationRequested();

        logger.LogInformation("The library has {SeriesCount} series and {VideoCount} videos.", seriesCount, videoCount);
        return Task.CompletedTask;
    }
}
