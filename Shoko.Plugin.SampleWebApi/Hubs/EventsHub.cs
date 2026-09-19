using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Shoko.Plugin.SampleWebApi.Hubs;

/// <summary>
/// Pushes <c>SeriesSummarized</c> messages to connected clients. Clients
/// authenticate with their API key as a bearer token, or as the
/// <c>access_token</c> query parameter.
/// </summary>
[Authorize]
public class EventsHub : Hub;
