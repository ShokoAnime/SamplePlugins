using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shoko.Abstractions.Core.Services;
using Shoko.Abstractions.Plugin;
using Shoko.Abstractions.Web.Attributes;
using Shoko.Plugin.SampleWebApi.Models;

namespace Shoko.Plugin.SampleWebApi.Controllers;

/// <summary>
/// An endpoint that answers in every server state.
/// </summary>
/// <remarks>
/// Every endpoint answers <c>503</c> until the server has finished starting,
/// unless it is marked <c>[InitFriendly]</c>, and <c>400</c> while the
/// database is blocked, unless it is marked <c>[DatabaseBlockedExempt]</c>.
/// Only exempt an endpoint that touches neither the database nor anything
/// built on it, as this one does.
/// </remarks>
/// <param name="systemService">The server state.</param>
/// <param name="pluginManager">Used to look up this plugin's version.</param>
[ApiController]
[AllowAnonymous]
[InitFriendly]
[DatabaseBlockedExempt]
[Route($"api/plugin/{Plugin.RouteNamespace}/Status")]
public class StatusController(ISystemService systemService, IPluginManager pluginManager) : ControllerBase
{
    /// <summary>
    /// Get the plugin version and the server state.
    /// </summary>
    /// <returns>The status.</returns>
    [HttpGet]
    public ActionResult<PluginStatus> GetStatus()
    {
        var version = pluginManager.GetPluginInfo<Plugin>()?.Version.Version.ToString(3) ?? "unknown";
        return new PluginStatus(version, systemService.IsStarted, systemService.InSetupMode, systemService.IsDatabaseBlocked);
    }
}
