using System;
using System.Collections.Frozen;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;
using Shoko.Abstractions.Web.Attributes;

namespace Shoko.Plugin.SampleDashboard.Controllers;

/// <summary>
/// Serves the dashboard files embedded in the plugin assembly.
/// </summary>
/// <remarks>
/// The files carry no data, so they are served to anyone, in every server
/// state: the page loads while the server is still starting, and asks the
/// API for everything else.
/// </remarks>
[ApiController]
[AllowAnonymous]
[InitFriendly]
[DatabaseBlockedExempt]
[ApiExplorerSettings(IgnoreApi = true)]
[Route($"plugin/{Plugin.RouteNamespace}")]
public class DashboardController : ControllerBase
{
    private const string ResourcePrefix = "Dashboard/";

    // Vite puts a content hash in every file name under assets/, so a
    // changed file is a new URL and the old one can be cached for good.
    // index.html keeps its name, so browsers check it with the ETag instead.
    private const string HashedCacheControl = "public, max-age=31536000, immutable";

    private const string UnhashedCacheControl = "no-cache";

    private static readonly FileExtensionContentTypeProvider _contentTypes = new();

    private static readonly Lazy<FrozenDictionary<string, Asset>> _assets = new(LoadAssets);

    /// <summary>
    /// Get a dashboard file. A path without an extension is a page of the
    /// dashboard itself, and gets <c>index.html</c>.
    /// </summary>
    /// <param name="path">The path below the dashboard root.</param>
    /// <returns>The file, <c>304</c> if the client's copy is current, or <c>404</c>.</returns>
    [HttpGet("{**path}")]
    public IActionResult GetFile([FromRoute] string? path = null)
    {
        if (_assets.Value.Count is 0)
            return NotFound("The dashboard was not built into this copy of the plugin.");

        path ??= string.Empty;
        if (!_assets.Value.TryGetValue(path, out var asset))
        {
            // A missing file with an extension is a real 404. Answering it
            // with the page would turn a missing script into a confusing
            // parse error in the browser.
            if (Path.HasExtension(path) || !_assets.Value.TryGetValue("index.html", out asset))
                return NotFound();
        }

        var acceptsGzip = Request.GetTypedHeaders().AcceptEncoding.Any(encoding => encoding.Value.Equals("gzip", StringComparison.OrdinalIgnoreCase) && encoding.Quality is not 0);
        Response.Headers.CacheControl = asset.IsHashed ? HashedCacheControl : UnhashedCacheControl;
        // The body depends on Accept-Encoding, so caches must key on it too.
        Response.Headers.Vary = HeaderNames.AcceptEncoding;

        // File() answers If-None-Match with a 304 on its own.
        if (acceptsGzip)
        {
            Response.Headers.ContentEncoding = "gzip";
            return File(asset.Compressed, asset.ContentType, lastModified: null, entityTag: asset.CompressedETag);
        }

        return File(asset.Decompressed.Value, asset.ContentType, lastModified: null, entityTag: asset.ETag);
    }

    #region Assets

    private static FrozenDictionary<string, Asset> LoadAssets()
    {
        var assembly = typeof(DashboardController).Assembly;
        return assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .ToFrozenDictionary(
                name => name[ResourcePrefix.Length..],
                name => LoadAsset(assembly.GetManifestResourceStream(name)!, name[ResourcePrefix.Length..]),
                StringComparer.Ordinal
            );
    }

    private static Asset LoadAsset(Stream stream, string path)
    {
        using var memory = new MemoryStream();
        using (stream)
            stream.CopyTo(memory);

        var compressed = memory.ToArray();
        var hash = Convert.ToHexStringLower(SHA256.HashData(compressed))[..16];
        return new Asset
        {
            ContentType = _contentTypes.TryGetContentType(path, out var contentType) ? contentType : "application/octet-stream",
            IsHashed = path.StartsWith("assets/", StringComparison.Ordinal),
            Compressed = compressed,
            // Two encodings of the same file are two different responses, so
            // they need two different ETags.
            ETag = new($"\"{hash}\""),
            CompressedETag = new($"\"{hash}-gzip\""),
            // Only a client that can't take gzip pays for this, once.
            Decompressed = new(() => Decompress(compressed)),
        };
    }

    private static byte[] Decompress(byte[] compressed)
    {
        using var input = new GZipStream(new MemoryStream(compressed), CompressionMode.Decompress);
        using var output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }

    private sealed class Asset
    {
        public required string ContentType { get; init; }

        public required bool IsHashed { get; init; }

        public required byte[] Compressed { get; init; }

        public required Lazy<byte[]> Decompressed { get; init; }

        public required EntityTagHeaderValue ETag { get; init; }

        public required EntityTagHeaderValue CompressedETag { get; init; }
    }

    #endregion
}
