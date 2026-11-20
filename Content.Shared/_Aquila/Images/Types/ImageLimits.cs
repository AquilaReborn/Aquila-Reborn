namespace Content.Shared._Aquila.Images.Types;

/// <summary>
/// Describes the size limits applied to an imported or exported PNG image.
/// </summary>
/// <param name="MaxBytes">Maximum size of the encoded image data in bytes.</param>
/// <param name="MaxDimension">
/// Maximum length of either image side in pixels. Zero or a negative value means the side is not limited.
/// </param>
public readonly record struct ImageLimits(int MaxBytes, int MaxDimension)
{
    /// <summary>
    /// Limits used for the paint canvas import.
    /// </summary>
    public static readonly ImageLimits Paint = new(2 * 1024 * 1024, 0);

    /// <summary>
    /// Limits used for the photo camera.
    /// </summary>
    public static readonly ImageLimits Photo = new(96 * 1024, 0);

    /// <summary>
    /// Limits used for the character profile image.
    /// </summary>
    public static readonly ImageLimits Profile = new(2 * 1024 * 1024, 2048);

    /// <summary>
    /// Checks whether an encoded image fits the byte limit.
    /// </summary>
    /// <param name="byteCount">Size of the encoded image data in bytes.</param>
    /// <returns><see langword="true"/> if the data fits the byte limit.</returns>
    public bool AllowsLength(int byteCount)
    {
        return byteCount <= MaxBytes;
    }

    /// <summary>
    /// Checks whether the given image side lengths fit the dimension limit.
    /// </summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <returns><see langword="true"/> if both sides fit the dimension limit.</returns>
    public bool AllowsDimension(int width, int height)
    {
        return MaxDimension <= 0 || (width <= MaxDimension && height <= MaxDimension);
    }
}
