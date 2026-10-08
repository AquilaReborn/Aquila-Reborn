using Content.Client._Aquila.PDA;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client._Aquila.AutoCook;

/// <summary>
/// Простая полоска прогресса для окна автоготовщика.
/// </summary>
public sealed class AutoCookBar : Control
{
    public float Value { get; set; }
    public Color BarColor { get; set; } = PdaStyle.Accent;

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = PixelSize;
        handle.DrawRect(new UIBox2(0, 0, size.X, size.Y), PdaStyle.BackgroundFloating);

        var width = size.X * Math.Clamp(Value, 0f, 1f);
        if (width > 0f)
            handle.DrawRect(new UIBox2(0, 0, width, size.Y), BarColor);
    }
}
