using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shoko.Abstractions.Video.Hashing;

namespace Shoko.Plugin.SampleProviders;

/// <summary>
/// Computes the OpenSubtitles hash: the file size plus the sum of the first
/// and last 64 KiB, read as 64-bit integers. It reads 128 KiB whatever the
/// size of the file, so it costs next to nothing.
/// </summary>
/// <remarks>
/// A new hash provider starts with none of its hash types enabled, and is not
/// called at all until a user enables one. Hash types are plain strings with
/// one owning provider each, so a bespoke digest should get a name nobody
/// else would pick. This one names a published algorithm, where two
/// providers computing it would agree on every value anyway. The provider ID
/// is derived from this type's full name, like the release provider's.
/// </remarks>
public class OpenSubtitlesHashProvider : IHashProvider
{
    /// <summary>
    /// The hash type this provider computes.
    /// </summary>
    public const string HashType = "OSHash";

    private const int ChunkSize = 64 * 1024;

    /// <inheritdoc/>
    public string Name => "OpenSubtitles Hash";

    /// <inheritdoc/>
    public string Description => "Computes the OpenSubtitles hash, used to look up subtitles for a file.";

    /// <inheritdoc/>
    /// <remarks>
    /// Read once at start-up. What the user has turned on arrives with each
    /// request instead.
    /// </remarks>
    public IReadOnlySet<string> AvailableHashTypes { get; } = new HashSet<string> { HashType };

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<HashDigest>> GetHashesForVideo(HashingRequest request, CancellationToken cancellationToken = default)
    {
        var (file, existingHashes, enabledHashTypes) = request;

        // Anything not enabled is dropped on the way back, so don't compute it.
        if (!enabledHashTypes.Contains(HashType))
            return [];

        if (existingHashes.FirstOrDefault(hash => hash.Type == HashType) is { } existing)
            return [new HashDigest { Type = existing.Type, Value = existing.Value, Metadata = existing.Metadata }];

        // The instance is shared, and called for several files at once, so it
        // keeps no state between calls.
        var value = await ComputeHash(file, cancellationToken).ConfigureAwait(false);
        return [new HashDigest { Type = HashType, Value = value }];
    }

    #region Helpers

    private static async Task<string> ComputeHash(FileInfo file, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1, useAsync: true);
        var length = stream.Length;
        var buffer = new byte[ChunkSize];
        var hash = unchecked((ulong)length);

        hash = unchecked(hash + await SumChunk(stream, 0, buffer, cancellationToken).ConfigureAwait(false));
        hash = unchecked(hash + await SumChunk(stream, Math.Max(0, length - ChunkSize), buffer, cancellationToken).ConfigureAwait(false));
        return hash.ToString("x16");
    }

    private static async Task<ulong> SumChunk(FileStream stream, long offset, byte[] buffer, CancellationToken cancellationToken)
    {
        stream.Position = offset;
        var read = await stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);

        // A short read at the end of a small file is padded with zeroes.
        Array.Clear(buffer, read, buffer.Length - read);
        var sum = 0UL;
        for (var index = 0; index < buffer.Length; index += sizeof(ulong))
            sum = unchecked(sum + BinaryPrimitives.ReadUInt64LittleEndian(buffer.AsSpan(index)));

        return sum;
    }

    #endregion
}
