using Content.Client._Aquila.PDA;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.PDA;

public sealed partial class PdaSheetlet
{
    private static StyleRule[] GetAquilaRules(NanotrasenStylesheet sheet)
    {
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

        return
        [
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
        ];
    }
}
