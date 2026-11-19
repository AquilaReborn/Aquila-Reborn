using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared.Chat;
using Content.Shared._EinsteinEngines.Language;
using Content.Shared._EinsteinEngines.Language.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Aquila.Chat.Controls;

/// <summary>
///     Строка ввода чата, в которой часть текста можно пометить другим языком.
/// </summary>
public sealed class LanguageLineEdit : HistoryLineEdit
{
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private static readonly LocId MenuTitleLoc = "aquila-chat-language-menu-title";
    private static readonly LocId MenuRemoveLoc = "aquila-chat-language-menu-remove";

    private readonly List<ChatMessageSegment> _segments = new();
    private readonly SegmentOverlay? _overlay;

    private string _trackedText = string.Empty;
    private Popup? _menu;
    private string _menuText = string.Empty;
    private int _menuSelectionLower;
    private int _menuSelectionUpper;

    public LanguageLineEdit()
    {
        IoCManager.InjectDependencies(this);

        var overlay = new SegmentOverlay(this);
        _overlay = overlay;
        AddChild(overlay);
        OnTextChanged += OnTextChangedAquila;
    }

    /// <summary>
    ///     Возвращает копию списка размеченных участков.
    /// </summary>
    public List<ChatMessageSegment> GetSegments()
    {
        SyncSegments();

        var copy = new List<ChatMessageSegment>(_segments.Count);
        foreach (var segment in _segments)
        {
            copy.Add(new ChatMessageSegment
            {
                Start = segment.Start,
                Length = segment.Length,
                Language = segment.Language
            });
        }

        return copy;
    }

    /// <summary>
    ///     Снимает все пометки языков.
    /// </summary>
    public void ClearSegments()
    {
        _segments.Clear();
        _trackedText = Text;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        SyncSegments();

        if (_menu != null && Text != _menuText)
            CloseMenu();
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var arranged = base.ArrangeOverride(finalSize);

        if (_overlay != null)
        {
            var box = UIBox2.FromDimensions(Vector2.Zero, finalSize);
            _overlay.Arrange(GetStyleBox().GetContentBox(box, 1));
        }

        return arranged;
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function != EngineKeyFunctions.UIClick || SelectionLength == 0)
            return;

        OpenLanguageMenu();
    }

    private void OnTextChangedAquila(LineEditEventArgs args)
    {
        SyncSegments();
    }

    private bool SyncSegments()
    {
        var text = Text;
        if (text == _trackedText)
            return false;

        var oldText = _trackedText;
        _trackedText = text;

        var max = Math.Min(oldText.Length, text.Length);
        var prefix = 0;
        while (prefix < max && oldText[prefix] == text[prefix])
            prefix++;

        var suffix = 0;
        while (suffix < max - prefix &&
               oldText[oldText.Length - 1 - suffix] == text[text.Length - 1 - suffix])
            suffix++;

        var changedStart = prefix;
        var changedEnd = oldText.Length - suffix;
        var delta = text.Length - oldText.Length;

        for (var i = _segments.Count - 1; i >= 0; i--)
        {
            var segment = _segments[i];
            var end = segment.Start + segment.Length;

            if (end <= changedStart)
                continue;

            if (segment.Start >= changedEnd)
            {
                segment.Start += delta;
                continue;
            }

            _segments.RemoveAt(i);
        }

        ClampSegments();
        return true;
    }

    private void ClampSegments()
    {
        var textLength = Text.Length;

        for (var i = _segments.Count - 1; i >= 0; i--)
        {
            var segment = _segments[i];
            var start = Math.Clamp(segment.Start, 0, textLength);
            var end = Math.Clamp(segment.Start + segment.Length, start, textLength);

            segment.Start = start;
            segment.Length = end - start;

            if (segment.Length <= 0)
                _segments.RemoveAt(i);
        }
    }

    private void MarkRange(int start, int end, string language)
    {
        var textLength = Text.Length;
        start = Math.Clamp(start, 0, textLength);
        end = Math.Clamp(end, start, textLength);

        if (start >= end)
            return;

        RemoveRange(start, end);
        _segments.Add(new ChatMessageSegment
        {
            Start = start,
            Length = end - start,
            Language = language
        });
        _segments.Sort(static (first, second) => first.Start.CompareTo(second.Start));
        MergeSegments();
    }

    private void RemoveRange(int start, int end)
    {
        var textLength = Text.Length;
        start = Math.Clamp(start, 0, textLength);
        end = Math.Clamp(end, start, textLength);

        if (start >= end)
            return;

        for (var i = _segments.Count - 1; i >= 0; i--)
        {
            var segment = _segments[i];
            var segmentEnd = segment.Start + segment.Length;

            if (segmentEnd <= start || segment.Start >= end)
                continue;

            if (segment.Start < start && segmentEnd > end)
            {
                _segments.Insert(i + 1, new ChatMessageSegment
                {
                    Start = end,
                    Length = segmentEnd - end,
                    Language = segment.Language
                });
                segment.Length = start - segment.Start;
                continue;
            }

            if (segment.Start < start)
            {
                segment.Length = start - segment.Start;
                continue;
            }

            if (segmentEnd > end)
            {
                segment.Start = end;
                segment.Length = segmentEnd - end;
                continue;
            }

            _segments.RemoveAt(i);
        }
    }

    private void MergeSegments()
    {
        for (var i = _segments.Count - 1; i > 0; i--)
        {
            var previous = _segments[i - 1];
            var current = _segments[i];

            if (previous.Language != current.Language || previous.Start + previous.Length != current.Start)
                continue;

            previous.Length += current.Length;
            _segments.RemoveAt(i);
        }
    }

    private bool HasMarkedRange(int start, int end)
    {
        foreach (var segment in _segments)
        {
            if (segment.Start < end && segment.Start + segment.Length > start)
                return true;
        }

        return false;
    }

    private void OpenLanguageMenu()
    {
        SyncSegments();

        var lower = SelectionLower;
        var upper = SelectionUpper;

        if (lower >= upper)
            return;

        if (_player.LocalEntity is not { } player)
            return;

        if (!_ent.TryGetComponent(player, out LanguageSpeakerComponent? speaker))
            return;

        CloseMenu();

        var container = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 1,
            MinWidth = 160,
            Margin = new Thickness(4)
        };

        container.AddChild(new Label
        {
            Text = Loc.GetString(MenuTitleLoc),
            StyleClasses = { StyleClass.LabelKeyText }
        });

        var languageCount = 0;

        foreach (var language in speaker.SpokenLanguages)
        {
            if (!_proto.TryIndex(language, out LanguagePrototype? proto))
                continue;

            var languageId = proto.ID;
            var button = new Button { Text = proto.Name };
            if (TryGetLanguageColor(languageId, out var color))
                button.Label.FontColorOverride = color;

            button.OnPressed += _ => ApplyLanguage(languageId);
            container.AddChild(button);
            languageCount++;
        }

        var hasMarkedRange = HasMarkedRange(lower, upper);

        if (hasMarkedRange)
        {
            var removeButton = new Button { Text = Loc.GetString(MenuRemoveLoc) };
            removeButton.OnPressed += _ => RemoveMarking();
            container.AddChild(removeButton);
        }

        if (languageCount == 0 && !hasMarkedRange)
            return;

        var panel = new PanelContainer
        {
            StyleClasses = { StyleClass.BorderedWindowPanel },
            MouseFilter = MouseFilterMode.Stop
        };
        panel.AddChild(container);

        var menu = new Popup();
        menu.AddChild(panel);
        menu.OnPopupHide += () => OnMenuHidden(menu);

        var origin = GlobalPosition;
        UserInterfaceManager.GetRootForMouse().ModalRoot.AddChild(menu);
        menu.Open(UIBox2.FromDimensions(origin, Vector2.One), altPos: origin, altPosUp: origin);

        _menu = menu;
        _menuText = Text;
        _menuSelectionLower = lower;
        _menuSelectionUpper = upper;
    }

    private void ApplyLanguage(string language)
    {
        MarkRange(_menuSelectionLower, _menuSelectionUpper, language);
        CloseMenu();
        GrabKeyboardFocus();
    }

    private void RemoveMarking()
    {
        RemoveRange(_menuSelectionLower, _menuSelectionUpper);
        CloseMenu();
        GrabKeyboardFocus();
    }

    private void CloseMenu()
    {
        _menu?.Close();
    }

    private void OnMenuHidden(Popup menu)
    {
        if (_menu == menu)
            _menu = null;

        menu.Orphan();
    }

    private StyleBox GetStyleBox()
    {
        if (StyleBoxOverride != null)
            return StyleBoxOverride;

        if (TryGetStyleProperty<StyleBox>(StylePropertyStyleBox, out var box))
            return box;

        return UserInterfaceManager.ThemeDefaults.LineEditBox;
    }

    private Font GetFont()
    {
        if (TryGetStyleProperty<Font>("font", out var font))
            return font;

        return UserInterfaceManager.ThemeDefaults.DefaultFont;
    }

    private bool TryGetLanguageColor(string language, out Color color)
    {
        color = default;

        if (!_proto.TryIndex<LanguagePrototype>(language, out var proto))
            return false;

        var speechColor = proto.SpeechOverride.Color;
        if (speechColor == null)
            return false;

        color = Color.InterpolateBetween(Color.White, speechColor.Value, speechColor.Value.A).WithAlpha(1f);
        return true;
    }

    private sealed class SegmentOverlay : Control
    {
        private readonly LanguageLineEdit _master;

        public SegmentOverlay(LanguageLineEdit master)
        {
            _master = master;

            RectClipContent = true;
            MouseFilter = MouseFilterMode.Ignore;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            var segments = _master._segments;
            var text = _master.Text;

            if (segments.Count == 0 || text.Length == 0)
                return;

            var font = _master.GetFont();
            var uiScale = UIScale;
            var contentBox = PixelSizeBox;
            var offsetY = (contentBox.Height - font.GetHeight(uiScale)) / 2;
            var baselineY = offsetY + font.GetAscent(uiScale);

            foreach (var segment in segments)
            {
                var start = Math.Clamp(segment.Start, 0, text.Length);
                var end = Math.Clamp(segment.Start + segment.Length, start, text.Length);

                if (start >= end)
                    continue;

                if (!_master.TryGetLanguageColor(segment.Language, out var color))
                    continue;

                var position = new Vector2(_master.GetOffsetAtIndex(start) * uiScale, baselineY);
                var runeIndex = 0;

                foreach (var rune in text.EnumerateRunes())
                {
                    if (runeIndex >= end)
                        break;

                    var runeStart = runeIndex;
                    runeIndex += rune.Utf16SequenceLength;

                    if (runeStart < start)
                        continue;

                    if (!font.TryGetCharMetrics(rune, uiScale, out var metrics))
                        continue;

                    if (position.X > contentBox.Right)
                        break;

                    if (position.X + metrics.BearingX + metrics.Width >= contentBox.Left)
                        font.DrawChar(handle, rune, position, uiScale, color);

                    position.X += metrics.Advance;
                }
            }
        }
    }
}
