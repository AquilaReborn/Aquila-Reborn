using Content.Client._Aquila.PDA;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.PDA;

public sealed partial class PdaNavigationButton
{
    private Thickness _borderThickness = new(0, 0, 0, 1);
    private Thickness _currentTabBorderThickness = new(0, 0, 0, 2);

    private readonly StyleBoxFlat _styleBox = new()
    {
        BackgroundColor = PdaStyle.BackgroundSecondary,
        BorderColor = PdaStyle.BackgroundTertiary,
        BorderThickness = new Thickness(0, 0, 0, 1)
    };

    public string InactiveBgColor { get; set; } = PdaStyle.BackgroundSecondaryHex;
    public string ActiveBgColor { get; set; } = PdaStyle.BackgroundHex;
    public string HoverBgColor { get; set; } = PdaStyle.HoverHex;
    public string InactiveFgColor { get; set; } = PdaStyle.TextDisabledHex;
    public string ActiveFgColor { get; set; } = PdaStyle.TextHeaderHex;
    public string IdleLabelColor { get; set; } = PdaStyle.TextInteractiveHex;
    public string HoverLabelColor { get; set; } = PdaStyle.TextNormalHex;
    public string BorderColor { get; set; } = PdaStyle.BackgroundTertiaryHex;
    public string CurrentBorderColor { get; set; } = PdaStyle.AccentHex;
    private bool _loaded;

    protected override void EnteredTree()
    {
        base.EnteredTree();
        UpdateStyle();
    }

    protected override void DrawModeChanged()
    {
        base.DrawModeChanged();
        UpdateStyle();
    }

    private void UpdateStyle()
    {
        if (!_loaded)
            return;

        var hovered = !_isCurrent && DrawMode == DrawModeEnum.Hover;

        _styleBox.BackgroundColor = Color.FromHex(_isCurrent ? ActiveBgColor : hovered ? HoverBgColor : InactiveBgColor);
        _styleBox.BorderColor = Color.FromHex(_isCurrent ? CurrentBorderColor : BorderColor);
        _styleBox.BorderThickness = _isCurrent ? _currentTabBorderThickness : _borderThickness;

        Icon.Modulate = Color.FromHex(_isActive ? ActiveFgColor : InactiveFgColor);

        var labelColor = !_isActive ? InactiveFgColor
            : _isCurrent ? ActiveFgColor
            : hovered ? HoverLabelColor
            : IdleLabelColor;
        Label.FontColorOverride = Color.FromHex(labelColor);
    }
}
