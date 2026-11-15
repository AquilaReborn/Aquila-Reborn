using Content.Client._Aquila.PDA;
using Content.Client.Stylesheets.Palette;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets.Sheetlets;

public sealed partial class ButtonSheetlet<T>
{
    private const string PdaButtonClass = "GreenPDAPalette";

    private static readonly ColorPalette PdaButtonPalette = new(
        Base: PdaStyle.Selected,
        LightnessShift: 0f,
        ChromaShift: 0f,
        Background: PdaStyle.Selected,
        BackgroundLight: PdaStyle.Hover,
        BackgroundDark: PdaStyle.BackgroundSecondary,
        Text: PdaStyle.TextHeader,
        TextDark: PdaStyle.TextMuted,
        Element: PdaStyle.Selected,
        HoveredElement: PdaStyle.AccentDark,
        PressedElement: PdaStyle.AccentDarker,
        DisabledElement: PdaStyle.BackgroundSecondary);

    private static void AddAquilaButtonRules(List<StyleRule> rules)
    {
        MakeButtonRules(rules, PdaButtonPalette, PdaButtonClass);
    }
}
