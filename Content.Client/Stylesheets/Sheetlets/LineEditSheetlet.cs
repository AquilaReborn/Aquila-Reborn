using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class LineEditSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, ILineEditConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        ILineEditConfig lineEditCfg = sheet;

        var lineEditStylebox = sheet.GetTextureOr(lineEditCfg.LineEditPath, NanotrasenStylesheet.TextureRoot)
            .IntoPatch(StyleBox.Margin.All, 3);
        lineEditStylebox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);

    var greenStyleBox = new StyleBoxFlat
    {
        BackgroundColor = Color.FromHex("#003300"),
        BorderColor = Color.FromHex("#006800"),
        BorderThickness = new Thickness(1),
    };

        return
        [
            E<LineEdit>()
                .Prop(LineEdit.StylePropertyStyleBox, lineEditStylebox),
            // TODO: Hardcoded colors bad, kill.
            E<LineEdit>()
                .Class(LineEdit.StyleClassLineEditNotEditable)
                .Prop("font-color", new Color(192, 192, 192)),
            E<LineEdit>()
                .Pseudo(LineEdit.StylePseudoClassPlaceholder)
                .Prop("font-color", Color.Gray),
            E<TextEdit>()
                .Pseudo(TextEdit.StylePseudoClassPlaceholder)
                .Prop("font-color", Color.Gray),
            E<LineEdit>()
                .Class("GreenLineEdit")
                .Prop(LineEdit.StylePropertyStyleBox, greenStyleBox)
                .Prop("font-color", new Color(0, 104, 0)),
        ];
    }
}
