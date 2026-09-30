using Robust.Client.Graphics;

namespace Content.Client._Aquila.PDA;

public static class PdaStyle
{
    public const string BackgroundHex = "#2E3530";
    public const string BackgroundSecondaryHex = "#272D29";
    public const string BackgroundTertiaryHex = "#1B201D";
    public const string BackgroundFloatingHex = "#202622";
    public const string HoverHex = "#333B35";
    public const string SelectedHex = "#3B4640";
    public const string SeparatorHex = "#35503E";
    public const string ScrollbarHoverHex = "#161B18";
    public const string TransparentHex = "#00000000";

    public const string TextHeaderHex = "#EEF5F0";
    public const string TextNormalHex = "#D5E0D8";
    public const string TextInteractiveHex = "#AEBDB2";
    public const string TextMutedHex = "#8C9C90";
    public const string TextDisabledHex = "#4A564D";

    public const string AccentHex = "#1E8C4C";
    public const string AccentDarkHex = "#1C6B3A";
    public const string AccentDarkerHex = "#14522C";

    public const string RedHex = "#F23F43";
    public const string YellowHex = "#F0B232";
    public const string GreenHex = AccentHex;

    public static readonly Color Background = Color.FromHex(BackgroundHex);
    public static readonly Color BackgroundSecondary = Color.FromHex(BackgroundSecondaryHex);
    public static readonly Color BackgroundTertiary = Color.FromHex(BackgroundTertiaryHex);
    public static readonly Color BackgroundFloating = Color.FromHex(BackgroundFloatingHex);
    public static readonly Color Hover = Color.FromHex(HoverHex);
    public static readonly Color Selected = Color.FromHex(SelectedHex);
    public static readonly Color Separator = Color.FromHex(SeparatorHex);
    public static readonly Color ScrollbarHover = Color.FromHex(ScrollbarHoverHex);

    public static readonly Color TextHeader = Color.FromHex(TextHeaderHex);
    public static readonly Color TextNormal = Color.FromHex(TextNormalHex);
    public static readonly Color TextInteractive = Color.FromHex(TextInteractiveHex);
    public static readonly Color TextMuted = Color.FromHex(TextMutedHex);
    public static readonly Color TextDisabled = Color.FromHex(TextDisabledHex);

    public static readonly Color Accent = Color.FromHex(AccentHex);
    public static readonly Color AccentDark = Color.FromHex(AccentDarkHex);
    public static readonly Color AccentDarker = Color.FromHex(AccentDarkerHex);

    public static readonly Color Red = Color.FromHex(RedHex);
    public static readonly Color Yellow = Color.FromHex(YellowHex);
    public static readonly Color Green = Color.FromHex(GreenHex);

    public static readonly Color[] AvatarColors =
    [
        Color.FromHex("#1E8C4C"),
        Color.FromHex("#2E7D5B"),
        Color.FromHex("#4F7F3A"),
        Color.FromHex("#3A6F5E"),
        Color.FromHex("#6B8E23"),
        Color.FromHex("#2F6F4F"),
    ];

    public static StyleBoxFlat Box(Color color, float contentMargin = 0)
    {
        var box = new StyleBoxFlat { BackgroundColor = color };
        box.SetContentMarginOverride(StyleBox.Margin.All, contentMargin);
        return box;
    }
}
