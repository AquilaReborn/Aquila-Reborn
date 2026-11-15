using System.IO;
using Content.Goobstation.Shared.Paint;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Goobstation.Client.Paint;

public sealed partial class PaintBoundUserInterface
{
    [Dependency] private IFileDialogManager _fileDialog = default!;

    private void InitializeAquila()
    {
        _input.FirstChanceOnKeyEvent += OnAquilaKeyEvent;

        if (_window == null)
            return;

        _window.ExportPressed += OnExportPressed;
        _window.ImportPressed += OnImportPressed;
    }

    private void DisposeAquila()
    {
        _input.FirstChanceOnKeyEvent -= OnAquilaKeyEvent;

        if (_window == null)
            return;

        _window.ExportPressed -= OnExportPressed;
        _window.ImportPressed -= OnImportPressed;
    }

    private void OnAquilaKeyEvent(KeyEventArgs keyEvent, KeyEventType type)
    {
        if (_window == null
            || type != KeyEventType.Down
            || keyEvent.Control
            || keyEvent.Alt
            || keyEvent.Shift
            || _uiManager.KeyboardFocused != null
            || !IsHoveringWindow())
            return;

        var tool = keyEvent.Key switch
        {
            Keyboard.Key.B => PaintWindowTool.Brush,
            Keyboard.Key.E => PaintWindowTool.Eraser,
            Keyboard.Key.L => PaintWindowTool.Line,
            Keyboard.Key.R => PaintWindowTool.Rectangle,
            Keyboard.Key.O => PaintWindowTool.Ellipse,
            Keyboard.Key.I => PaintWindowTool.Picker,
            _ => (PaintWindowTool?) null,
        };

        if (tool is { } value)
        {
            keyEvent.Handle();
            _window.AquilaSelectTool(value);
            return;
        }

        var delta = keyEvent.Key switch
        {
            Keyboard.Key.LBracket => -1,
            Keyboard.Key.RBracket => 1,
            _ => 0,
        };

        if (delta != 0)
        {
            keyEvent.Handle();
            _window.AquilaAdjustBrushSize(delta);
        }
    }

    private async void OnExportPressed()
    {
        if (GetImage() is not { } image)
            return;

        var data = EncodeToPng(image);
        var file = await _fileDialog.SaveFile(new FileDialogFilters(new FileDialogFilters.Group("png")));
        if (file is not { } result)
            return;

        try
        {
            await result.fileStream.WriteAsync(data, 0, data.Length);
        }
        finally
        {
            await result.fileStream.DisposeAsync();
        }
    }

    private async void OnImportPressed()
    {
        var stream = await _fileDialog.OpenFile(new FileDialogFilters(new FileDialogFilters.Group("png")), FileAccess.Read);
        if (stream == null)
            return;

        await using (stream)
        {
            if (stream.Length > PaintImportMessage.MaxImportBytes)
                return;

            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            SendMessage(new PaintImportMessage(memory.ToArray()));
        }
    }

    private static byte[] EncodeToPng(PaintImage image)
    {
        var buffer = new Rgba32[image.Width * image.Height];
        PaintTextures.Fill(image, buffer);

        using var stream = new MemoryStream();
        using (var png = new Image<Rgba32>(image.Width, image.Height))
        {
            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    png[x, y] = buffer[y * image.Width + x];
                }
            }

            png.SaveAsPng(stream);
        }

        return stream.ToArray();
    }
}
