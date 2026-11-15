namespace Content.Goobstation.Client.Paint;

public sealed partial class PaintWindow
{
    public event Action? ExportPressed;
    public event Action? ImportPressed;

    internal void AquilaSelectTool(PaintWindowTool tool)
    {
        if (!HasTool || !_toolButtons.TryGetValue(tool, out var button) || button.Disabled)
            return;

        button.Pressed = true;
        SelectTool(tool);
    }

    internal void AquilaAdjustBrushSize(int delta)
    {
        if (!HasTool)
            return;

        SizeSlider.Value = Math.Clamp(SizeSlider.Value + delta, SizeSlider.MinValue, SizeSlider.MaxValue);
    }

    private void InitializeAquila()
    {
        ExportButton.OnPressed += _ => ExportPressed?.Invoke();
        ImportButton.OnPressed += _ => ImportPressed?.Invoke();
    }
}
