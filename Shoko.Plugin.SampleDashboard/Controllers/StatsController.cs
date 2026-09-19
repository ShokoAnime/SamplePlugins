using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shoko.Abstractions.Metadata.Services;
using Shoko.Abstractions.User.Services;
using Shoko.Abstractions.Video.Services;
using Shoko.Plugin.SampleDashboard.Models;

namespace Shoko.Plugin.SampleDashboard.Controllers;

/// <summary>
/// The dashboard's API. Unlike the page, it needs an API key.
/// </summary>
/// <param name="metadataService">Used to count the series.</param>
/// <param name="videoService">Used to count the videos.</param>
/// <param name="userService">Used to find the user behind the request.</param>
[ApiController]
[Authorize]
[Route($"api/plugin/{Plugin.RouteNamespace}/Stats")]
public class StatsController(IMetadataService metadataService, IVideoService videoService, IUserService userService) : ControllerBase
{
    /// <summary>
    /// Get the numbers the dashboard shows.
    /// </summary>
    /// <returns>The numbers.</returns>
    [HttpGet]
    public ActionResult<DashboardStats> GetStats()
    {
        var userName = userService.GetUserFromHttpContext(HttpContext)?.Username ?? string.Empty;
        return new DashboardStats(userName, metadataService.GetAllShokoSeries().Count(), videoService.GetAllVideos().Count());
    }
}
