using Content.Client._Aquila.PDA; // Aquila Change
using Content.Client.PDA;
using Content.Client.Stylesheets.Fonts; // Aquila Change
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Sheetlets;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.PDA;

[CommonSheetlet]
public sealed class PdaSheetlet : Sheetlet<NanotrasenStylesheet>
{
    public override StyleRule[] GetRules(NanotrasenStylesheet sheet, object config)
    {
        IPanelConfig panelCfg = sheet;
        IButtonConfig btnCfg = sheet;

        // TODO: This should have its own set of images, instead of using button cfg directly.
        var angleBorderRect =
            sheet.GetTexture(panelCfg.GeometricPanelBorderPath).IntoPatch(StyleBox.Margin.All, 10);

        // Aquila Change start
        var flatButton = PdaStyle.Box(Color.White);
        flatButton.SetContentMarginOverride(StyleBox.Margin.Horizontal, 8);
        flatButton.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);

        var cardRow = PdaStyle.Box(Color.Transparent);
        var cardRowHover = PdaStyle.Box(PdaStyle.Hover);
        var cardRowPressed = PdaStyle.Box(PdaStyle.Selected);
        foreach (var box in new[] { cardRow, cardRowHover, cardRowPressed })
        {
            box.SetContentMarginOverride(StyleBox.Margin.Horizontal, 8);
            box.SetContentMarginOverride(StyleBox.Margin.Vertical, 4);
        }

        var listEntry = PdaStyle.Box(Color.White, 4);

        var scrollGrabber = new StyleBoxFlat
        {
            BackgroundColor = PdaStyle.BackgroundTertiary,
            ContentMarginLeftOverride = 6,
            ContentMarginTopOverride = 6,
        };
        var scrollGrabberHover = new StyleBoxFlat(scrollGrabber) { BackgroundColor = PdaStyle.ScrollbarHover };
        var scrollGrabberPressed = new StyleBoxFlat(scrollGrabber) { BackgroundColor = PdaStyle.Selected };
        // Aquila Change end

        return
        [
            //PDA - Backgrounds
            E<PanelContainer>()
                .Class("PdaContentBackground")
                .Prop(PanelContainer.StylePropertyPanel, StyleBoxHelpers.SquareStyleBox(sheet))
                .Prop(Control.StylePropertyModulateSelf, PdaStyle.Background), // Aquila Change

            E<PanelContainer>()
                .Class("PdaBackground")
                .Prop(PanelContainer.StylePropertyPanel, StyleBoxHelpers.SquareStyleBox(sheet))
                .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#000000")),

            E<PanelContainer>()
                .Class("PdaBackgroundRect")
                .Prop(PanelContainer.StylePropertyPanel, StyleBoxHelpers.BaseStyleBox((sheet)))
                .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#024e15")),

            E<PanelContainer>()
                .Class("PdaBorderRect")
                .Prop(PanelContainer.StylePropertyPanel, angleBorderRect),

            //PDA - Buttons
            E<PdaSettingsButton>()
                .Pseudo(ContainerButton.StylePseudoClassNormal)
                .Prop(PdaSettingsButton.StylePropertyBgColor, Color.FromHex(PdaSettingsButton.NormalBgColor))
                .Prop(PdaSettingsButton.StylePropertyFgColor, Color.FromHex(PdaSettingsButton.EnabledFgColor)),

            E<PdaSettingsButton>()
                .Pseudo(ContainerButton.StylePseudoClassHover)
                .Prop(PdaSettingsButton.StylePropertyBgColor, Color.FromHex(PdaSettingsButton.HoverColor))
                .Prop(PdaSettingsButton.StylePropertyFgColor, Color.FromHex(PdaSettingsButton.EnabledFgColor)),

            E<PdaSettingsButton>()
                .Pseudo(ContainerButton.StylePseudoClassPressed)
                .Prop(PdaSettingsButton.StylePropertyBgColor, Color.FromHex(PdaSettingsButton.PressedColor))
                .Prop(PdaSettingsButton.StylePropertyFgColor, Color.FromHex(PdaSettingsButton.EnabledFgColor)),

            E<PdaSettingsButton>()
                .Pseudo(ContainerButton.StylePseudoClassDisabled)
                .Prop(PdaSettingsButton.StylePropertyBgColor, Color.FromHex(PdaSettingsButton.NormalBgColor))
                .Prop(PdaSettingsButton.StylePropertyFgColor, Color.FromHex(PdaSettingsButton.DisabledFgColor)),

            E<PdaProgramItem>()
                .Pseudo(ContainerButton.StylePseudoClassNormal)
                .Prop(PdaProgramItem.StylePropertyBgColor, Color.FromHex(PdaProgramItem.NormalBgColor)),

            E<PdaProgramItem>()
                .Pseudo(ContainerButton.StylePseudoClassHover)
                .Prop(PdaProgramItem.StylePropertyBgColor, Color.FromHex(PdaProgramItem.HoverColor)),

            E<PdaProgramItem>()
                .Pseudo(ContainerButton.StylePseudoClassPressed)
                .Prop(PdaProgramItem.StylePropertyBgColor, Color.FromHex(PdaProgramItem.HoverColor)),

            //PDA - Text
            E<Label>()
                .Class("PdaContentFooterText")
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(10))
                .Prop(Label.StylePropertyFontColor, PdaStyle.TextMuted), // Aquila Change

            E<Label>()
                .Class("PdaWindowFooterText")
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(10))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#006800")),

            // Aquila Change start
            E<PanelContainer>()
                .Class("PdaCard")
                .Panel(PdaStyle.Box(PdaStyle.BackgroundSecondary, 4)),

            E<PanelContainer>()
                .Class("PdaSeparator")
                .Panel(PdaStyle.Box(PdaStyle.Separator)),

            E<ContainerButton>().Class("PdaCardRow").PseudoNormal().Box(cardRow),
            E<ContainerButton>().Class("PdaCardRow").PseudoHovered().Box(cardRowHover),
            E<ContainerButton>().Class("PdaCardRow").PseudoPressed().Box(cardRowPressed),

            E<Label>()
                .Class("PdaSectionHeader")
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(10, FontKind.Bold))
                .Prop(Label.StylePropertyFontColor, PdaStyle.TextMuted),

            E<Label>()
                .Class("PdaSettingsDescription")
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(10)),

            E<ContainerButton>().Class(ContainerButton.StyleClassButton).Class("GreenPDAPalette").PseudoNormal().Box(flatButton),
            E<ContainerButton>().Class(ContainerButton.StyleClassButton).Class("GreenPDAPalette").PseudoHovered().Box(flatButton),
            E<ContainerButton>().Class(ContainerButton.StyleClassButton).Class("GreenPDAPalette").PseudoPressed().Box(flatButton),
            E<ContainerButton>().Class(ContainerButton.StyleClassButton).Class("GreenPDAPalette").PseudoDisabled().Box(flatButton),

            E<ContainerButton>().Class(ContainerButton.StyleClassButton).Class("PdaListEntry").PseudoNormal().Box(listEntry).Prop(Control.StylePropertyModulateSelf, PdaStyle.BackgroundSecondary),
            E<ContainerButton>().Class(ContainerButton.StyleClassButton).Class("PdaListEntry").PseudoHovered().Box(listEntry).Prop(Control.StylePropertyModulateSelf, PdaStyle.Hover),
            E<ContainerButton>().Class(ContainerButton.StyleClassButton).Class("PdaListEntry").PseudoPressed().Box(listEntry).Prop(Control.StylePropertyModulateSelf, PdaStyle.Selected),
            E<ContainerButton>().Class(ContainerButton.StyleClassButton).Class("PdaListEntry").PseudoDisabled().Box(listEntry).Prop(Control.StylePropertyModulateSelf, PdaStyle.BackgroundSecondary),

            E<ScrollContainer>().Class("PdaScroll").ParentOf(E<VScrollBar>()).Prop(ScrollBar.StylePropertyGrabber, scrollGrabber),
            E<ScrollContainer>().Class("PdaScroll").ParentOf(E<VScrollBar>().PseudoHovered()).Prop(ScrollBar.StylePropertyGrabber, scrollGrabberHover),
            E<ScrollContainer>().Class("PdaScroll").ParentOf(E<VScrollBar>().PseudoPressed()).Prop(ScrollBar.StylePropertyGrabber, scrollGrabberPressed),
            // Aquila Change end
        ];
    }
}

