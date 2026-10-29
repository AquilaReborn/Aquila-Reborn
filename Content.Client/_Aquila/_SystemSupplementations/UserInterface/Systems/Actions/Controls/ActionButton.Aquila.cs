using Content.Client._Aquila.Actions;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Palette;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Actions.Controls;

public sealed partial class ActionButton
{
    private const string WhiteTextColor = "whiteText";
    private static readonly Color CooldownModulate = Palettes.Red.Base;

    [Dependency] private readonly IGameTiming _timing = default!;

    private ActionCustomizationUIController? _customization;
    private int? _shownCooldownSeconds;

    private readonly Label _cooldownLabel = new()
    {
        HorizontalAlignment = HAlignment.Center,
        VerticalAlignment = VAlignment.Center,
        StyleClasses = { StyleClass.LabelHeading },
        FontColorShadowOverride = Color.Black,
        Visible = false,
    };

    public readonly Label CustomBindLabel = new()
    {
        HorizontalAlignment = HAlignment.Stretch,
        VerticalAlignment = VAlignment.Bottom,
        Margin = new Thickness(5, 0, 5, 0),
        ClipText = true,
        StyleClasses = { StyleClass.LabelKeyText },
        FontColorShadowOverride = Color.Black,
        Visible = false,
    };

    private ActionCustomizationUIController Customization =>
        _customization ??= UserInterfaceManager.GetUIController<ActionCustomizationUIController>();

    private void InitializeAquila()
    {
        IoCManager.InjectDependencies(this);

        AddChild(_cooldownLabel);
        AddChild(CustomBindLabel);
    }

    private void UpdateAquilaTheme()
    {
        _cooldownLabel.FontColorOverride = Theme.ResolveColorOrSpecified(WhiteTextColor);
    }

    private bool TryHandleAquilaPress(GUIBoundKeyEventArgs args)
    {
        if (!ActionCustomizationUIController.IsShiftClick(args.Function))
            return false;

        Customization.OnShiftPressed(args, this);
        return true;
    }

    private void UpdateAquilaData()
    {
        Customization.UpdateBindLabel(this);
    }

    private void UpdateCooldownDisplay()
    {
        var remaining = Action?.Comp.Cooldown is { } cooldown ? cooldown.End - _timing.CurTime : TimeSpan.Zero;
        var onCooldown = remaining > TimeSpan.Zero;
        var tint = onCooldown ? CooldownModulate : Color.White;
        var iconColor = Action?.Comp.IconColor ?? Color.White;

        _bigActionIcon.Modulate = iconColor * tint;
        _smallActionIcon.Modulate = iconColor * tint;
        _bigItemSpriteView.Modulate = tint;
        _smallItemSpriteView.Modulate = tint;
        _cooldownLabel.Visible = onCooldown;

        if (!onCooldown)
        {
            _shownCooldownSeconds = null;
            return;
        }

        var seconds = (int) Math.Ceiling(remaining.TotalSeconds);
        if (_shownCooldownSeconds == seconds)
            return;

        _shownCooldownSeconds = seconds;
        var shown = TimeSpan.FromSeconds(seconds);
        _cooldownLabel.Text = shown < TimeSpan.FromMinutes(1)
            ? seconds.ToString()
            : $"{(int) shown.TotalMinutes}:{shown.Seconds:00}";
    }
}
