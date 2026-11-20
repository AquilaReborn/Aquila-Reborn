using System.Buffers.Binary;
using Content.Shared._Aquila.Images.Types;

namespace Content.Shared._Aquila.Images.Helpers;

/// <summary>
/// Reads and validates PNG image headers.
/// </summary>
public static class PngHelper
{
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private static readonly byte[] IhdrChunk = { 0x49, 0x48, 0x44, 0x52 };

    /// <summary>
    /// Checks whether the data starts with the PNG signature.
    /// </summary>
    /// <param name="data">Encoded image data.</param>
    /// <returns><see langword="true"/> if the data starts with the PNG signature.</returns>
    public static bool HasPngSignature(ReadOnlySpan<byte> data)
    {
        return data.StartsWith(PngSignature);
    }

    /// <summary>
    /// Reads the width and height from the PNG header without decoding the image.
    /// </summary>
    /// <param name="data">Encoded image data.</param>
    /// <param name="width">Width of the image in pixels, or zero if the dimensions are not available.</param>
    /// <param name="height">Height of the image in pixels, or zero if the dimensions are not available.</param>
    /// <returns><see langword="true"/> if the dimensions were read.</returns>
    public static bool TryGetPngDimensions(ReadOnlySpan<byte> data, out int width, out int height)
    {
        const int signatureLength = 8;
        const int chunkLengthSize = 4;
        const int chunkTypeSize = 4;
        const int dimensionSize = 4;
        const int chunkTypeOffset = signatureLength + chunkLengthSize;
        const int widthOffset = chunkTypeOffset + chunkTypeSize;
        const int heightOffset = widthOffset + dimensionSize;
        const int headerLength = heightOffset + dimensionSize;

        width = 0;
        height = 0;

        if (data.Length < headerLength || !HasPngSignature(data))
            return false;

        if (!data.Slice(chunkTypeOffset, chunkTypeSize).SequenceEqual(IhdrChunk))
            return false;

        var parsedWidth = BinaryPrimitives.ReadInt32BigEndian(data.Slice(widthOffset, dimensionSize));
        var parsedHeight = BinaryPrimitives.ReadInt32BigEndian(data.Slice(heightOffset, dimensionSize));
        if (parsedWidth <= 0 || parsedHeight <= 0)
            return false;

        width = parsedWidth;
        height = parsedHeight;
        return true;
    }

    /// <summary>
    /// Checks whether the given data is a PNG image accepted for the character profile.
    /// </summary>
    /// <param name="data">Encoded image data, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the data is a PNG image within the profile limits.</returns>
    public static bool TryValidateProfileImage(byte[]? data)
    {
        if (data == null
            || !ImageLimits.Profile.AllowsLength(data.Length)
            || !HasPngSignature(data)
            || !TryGetPngDimensions(data, out var width, out var height))
            return false;

        return ImageLimits.Profile.AllowsDimension(width, height);
    }
}
