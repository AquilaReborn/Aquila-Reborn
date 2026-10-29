using Content.Client._Aquila.PDA;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.PDA;

public sealed partial class PdaProgramItem
{
    private readonly StyleBoxFlat _styleBox = PdaStyle.Box(PdaStyle.BackgroundSecondary);

    public Color BackgroundColor
    {
        get => _styleBox.BackgroundColor;
        set => _styleBox.BackgroundColor = value;
    }

    public PdaProgramItem()
    {
        RobustXamlLoader.Load(this);
        Panel.PanelOverride = _styleBox;
        IconBackground.PanelOverride = PdaStyle.Box(PdaStyle.Selected);
        ProgramName.FontColorOverride = PdaStyle.TextHeader;
    }
}
