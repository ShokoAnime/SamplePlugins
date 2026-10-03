using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Shoko.Abstractions.Config;

namespace Shoko.Plugin.SampleConfiguration;

/// <summary>
/// A client for the made-up remote service.
/// </summary>
/// <param name="httpClient">The HTTP client, from the client factory.</param>
/// <param name="configurationProvider">The provider for the plugin configuration.</param>
public class SampleApiClient(HttpClient httpClient, ConfigurationProvider<SampleConfiguration> configurationProvider)
{
    /// <summary>
    /// Checks whether the service at <paramref name="serverAddress"/> accepts
    /// <paramref name="apiKey"/>.
    /// </summary>
    /// <param name="serverAddress">The address of the service.</param>
    /// <param name="apiKey">The API key to try.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns><c>true</c> if the key was accepted, <c>false</c> if it was refused.</returns>
    /// <exception cref="HttpRequestException">The service could not be reached, or answered with an unexpected error.</exception>
    /// <exception cref="TaskCanceledException">The request timed out, or was cancelled.</exception>
    public async Task<bool> CheckConnection(Uri serverAddress, string apiKey, CancellationToken cancellationToken = default)
    {
        // Read the configuration here, where it is used. A value copied into a
        // field at startup would ignore every change the user saves later.
        var configuration = configurationProvider.Load();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(configuration.RequestTimeoutSeconds));

        using var request = new HttpRequestMessage(HttpMethod.Get, serverAddress);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        using var response = await httpClient.SendAsync(request, timeout.Token).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return false;

        response.EnsureSuccessStatusCode();
        return true;
    }
}
