using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shoko.Abstractions.Actions.Services;
using Shoko.Abstractions.Metadata.Services;
using Shoko.Abstractions.User.Services;
using Shoko.Abstractions.Utilities;
using Shoko.Plugin.SampleWebApi.Actions;
using Shoko.Plugin.SampleWebApi.Models;

namespace Shoko.Plugin.SampleWebApi.Controllers;

/// <summary>
/// Series endpoints. Every request needs an API key, sent as the
/// <c>apikey</c> header or query parameter.
/// </summary>
/// <param name="metadataService">Used to look series up.</param>
/// <param name="actionService">Used to run the plugin's series action.</param>
/// <param name="userService">Used to find the user behind the request.</param>
[ApiController]
[Authorize]
[Route($"api/plugin/{Plugin.RouteNamespace}/Series")]
public class SeriesController(IMetadataService metadataService, IActionService actionService, IUserService userService) : ControllerBase
{
    private readonly Guid _summarizeActionID = actionService.GetActionInfo<SummarizeSeriesAction>()!.Id;

    /// <summary>
    /// Get a summary of one series.
    /// </summary>
    /// <param name="seriesID">The Shoko series ID.</param>
    /// <returns>The summary.</returns>
    [HttpGet("{seriesID:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<SeriesSummary> GetSeries([FromRoute] int seriesID)
    {
        if (metadataService.GetShokoSeriesByID(seriesID) is not { } series)
            return NotFound();

        return new SeriesSummary(series.ID, series.PreferredTitle?.Value ?? series.Title, series.Episodes.Count, series.Videos.Count);
    }

    /// <summary>
    /// Queue the summarize action for one series, as the calling user.
    /// </summary>
    /// <param name="seriesID">The Shoko series ID.</param>
    /// <param name="cancellationToken">The request's cancellation token.</param>
    /// <returns><c>202</c> once queued, <c>400</c> with the reason if the action refused.</returns>
    [HttpPost("{seriesID:int}/Summarize")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SummarizeSeries([FromRoute] int seriesID, CancellationToken cancellationToken)
    {
        if (metadataService.GetShokoSeriesByID(seriesID) is not { } series)
            return NotFound();

        // Passing the user applies the action's permission check. A null
        // caller would skip it, which is only right for trusted server code.
        var user = userService.GetUserFromHttpContext(HttpContext);
        if (await actionService.InvokeAsync(_summarizeActionID, series, user, cancellationToken) is { } rejected)
            return BadRequest(rejected.Reason);

        return Accepted();
    }
}
