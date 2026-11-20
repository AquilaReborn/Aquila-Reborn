using System.IO;
using System.Threading.Tasks;
using Content.Shared._Aquila.Images.Helpers;
using Content.Shared._Aquila.Images.Types;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Content.Client._Aquila.Images.Systems;

/// <summary>
/// Imports and exports PNG images and caches their textures.
/// </summary>
public sealed class ImageTransferSystem : EntitySystem
{
    [Dependency] private readonly IFileDialogManager _fileDialog = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private const int MaxCachedTextures = 64;

    private readonly Dictionary<byte[], OwnedTexture> _textures = new();
    private readonly Dictionary<(byte[] Data, int MaxSide), OwnedTexture> _previews = new();

    private ISawmill _sawmill = default!;
    private int _textureNameCounter;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("aquila.images");
    }

    public override void Shutdown()
    {
        base.Shutdown();

        ClearTextureCache();
        ClearPreviewCache();
    }

    private void ClearTextureCache()
    {
        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }

        _textures.Clear();
    }

    private void ClearPreviewCache()
    {
        foreach (var preview in _previews.Values)
        {
            preview.Dispose();
        }

        _previews.Clear();
    }

    /// <summary>
    /// Lets the user pick a PNG file and returns its bytes if the file fits the given limits.
    /// </summary>
    /// <remarks>
    /// Only the byte limit and the PNG signature are checked here. Callers can check the image dimensions
    /// with <see cref="PngHelper.TryGetPngDimensions"/> and <see cref="ImageLimits.AllowsDimension"/>.
    /// </remarks>
    /// <param name="limits">Limits applied to the imported file.</param>
    /// <returns>The imported PNG data, or <see langword="null"/> if the import was cancelled or the file is not accepted.</returns>
    public async Task<byte[]?> ImportPngAsync(ImageLimits limits)
    {
        Stream? stream;
        try
        {
            stream = await _fileDialog.OpenFile(new FileDialogFilters(new FileDialogFilters.Group("png")), FileAccess.Read);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to open the PNG dialog: {e}");
            return null;
        }

        if (stream == null)
            return null;

        await using (stream)
        {
            if (stream.CanSeek && stream.Length > limits.MaxBytes)
                return null;

            byte[] data;
            try
            {
                using var memory = new MemoryStream();
                await stream.CopyToAsync(memory);
                data = memory.ToArray();
            }
            catch (Exception e)
            {
                _sawmill.Error($"Failed to read the PNG file: {e}");
                return null;
            }

            if (data.Length == 0
                || !limits.AllowsLength(data.Length)
                || !PngHelper.HasPngSignature(data))
                return null;

            return data;
        }
    }

    /// <summary>
    /// Saves the given PNG data to a file chosen by the user.
    /// </summary>
    /// <param name="data">PNG data to save.</param>
    /// <returns><see langword="true"/> if the file was saved.</returns>
    public async Task<bool> ExportPngAsync(byte[] data)
    {
        (Stream fileStream, bool alreadyExisted)? file;
        try
        {
            file = await _fileDialog.SaveFile(new FileDialogFilters(new FileDialogFilters.Group("png")));
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to open the PNG save dialog: {e}");
            return false;
        }

        if (file is not { } result)
            return false;

        try
        {
            await result.fileStream.WriteAsync(data, 0, data.Length);
            return true;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to write the PNG file: {e}");
            return false;
        }
        finally
        {
            await result.fileStream.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a texture from the given PNG data, reusing a cached texture for the same data.
    /// </summary>
    /// <param name="data">PNG data to decode.</param>
    /// <param name="texture">The created or cached texture, or <see langword="null"/> if the data is not a valid PNG.</param>
    /// <returns><see langword="true"/> if the texture is available.</returns>
    public bool TryCreateTexture(byte[] data, out Texture? texture)
    {
        texture = null;

        if (!PngHelper.HasPngSignature(data))
            return false;

        if (_textures.TryGetValue(data, out var cached))
        {
            texture = cached;
            return true;
        }

        if (_textures.Count >= MaxCachedTextures)
            ClearTextureCache();

        try
        {
            using var stream = new MemoryStream(data, false);
            if (Texture.LoadFromPNGStream(stream, $"aquila-image-{_textureNameCounter++}") is not OwnedTexture loaded)
                return false;

            _textures[data] = loaded;
            texture = loaded;
            return true;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to decode the PNG image: {e}");
            return false;
        }
    }

    /// <summary>
    /// Creates a downscaled texture for the given PNG data, reusing a cached preview for the same data and side limit.
    /// </summary>
    /// <param name="data">PNG data to decode.</param>
    /// <param name="maxSide">Maximum length of the longest side of the preview in pixels.</param>
    /// <param name="texture">The created or cached preview texture, or <see langword="null"/> if the preview is not available.</param>
    /// <returns><see langword="true"/> if the preview texture is available.</returns>
    public bool TryCreatePreviewTexture(byte[] data, int maxSide, out Texture? texture)
    {
        texture = null;

        if (maxSide <= 0 || !PngHelper.HasPngSignature(data))
            return false;

        var key = (data, maxSide);
        if (_previews.TryGetValue(key, out var cached))
        {
            texture = cached;
            return true;
        }

        if (_previews.Count >= MaxCachedTextures)
            ClearPreviewCache();

        try
        {
            using var stream = new MemoryStream(data, false);
            using var image = Image.Load<Rgba32>(stream);
            var longestSide = Math.Max(image.Width, image.Height);
            if (longestSide <= 0)
                return false;

            OwnedTexture? loaded;
            if (longestSide > maxSide)
            {
                var scale = (double) maxSide / longestSide;
                var width = Math.Max(1, (int) Math.Round(image.Width * scale));
                var height = Math.Max(1, (int) Math.Round(image.Height * scale));
                using var resized = image.Clone(context => context.Resize(width, height));
                loaded = Texture.LoadFromImage(resized, $"aquila-preview-{_textureNameCounter++}") as OwnedTexture;
            }
            else
            {
                loaded = Texture.LoadFromImage(image, $"aquila-preview-{_textureNameCounter++}") as OwnedTexture;
            }

            if (loaded == null)
                return false;

            _previews[key] = loaded;
            texture = loaded;
            return true;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to create the PNG preview: {e}");
            return false;
        }
    }

    /// <summary>
    /// Encodes raw RGBA pixels into PNG data.
    /// </summary>
    /// <param name="pixels">Row-major RGBA pixels.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <returns>Encoded PNG data.</returns>
    public byte[] EncodePng(ReadOnlySpan<Rgba32> pixels, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image[x, y] = pixels[y * width + x];
            }
        }

        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }
}
