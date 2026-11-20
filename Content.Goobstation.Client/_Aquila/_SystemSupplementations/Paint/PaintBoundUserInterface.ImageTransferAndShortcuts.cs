using Content.Client._Aquila.Images.Systems;
using Content.Goobstation.Shared.Paint;
using Content.Shared._Aquila.Images.Types;
using Robust.Client.Input;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Goobstation.Client.Paint;

public sealed partial class PaintBoundUserInterface
{
    private ImageTransferSystem _images = default!;

    private void InitializeAquila()
    {
        _images = EntMan.System<ImageTransferSystem>();
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

        await _images.ExportPngAsync(EncodeToPng(image));
    }

    private async void OnImportPressed()
    {
        if (await _images.ImportPngAsync(ImageLimits.Paint) is not { } data)
            return;

        SendMessage(new PaintImportMessage(data));
    }

    private byte[] EncodeToPng(PaintImage image)
    {
        var buffer = new Rgba32[image.Width * image.Height];
        PaintTextures.Fill(image, buffer);
        return _images.EncodePng(buffer, image.Width, image.Height);
    }
}
