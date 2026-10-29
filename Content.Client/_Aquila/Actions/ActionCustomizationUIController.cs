using System.Linq;
using System.Numerics;
using Content.Client._Aquila.Actions.DataDefinitions;
using Content.Client.Actions;
using Content.Client.Gameplay;
using Content.Client.Interaction;
using Content.Client.Popups;
using Content.Client.UserInterface.Systems.Actions;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Content.Client.UserInterface.Systems.Actions.Widgets;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared.Actions.Components;
using Content.Shared.Input;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.ContentPack;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client._Aquila.Actions;

/// <summary>
/// Выносит кнопки действий на экран и назначает действиям собственные бинды по Shift + ЛКМ.
/// </summary>
public sealed class ActionCustomizationUIController : UIController, IOnStateChanged<GameplayState>, IOnSystemChanged<ActionsSystem>
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IResourceManager _resource = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    [UISystemDependency] private readonly ActionsSystem? _actionsSystem = default;
    [UISystemDependency] private readonly PopupSystem? _popup = default;

    private static readonly ResPath SavePath = new("/aquila_action_customization.yml");
    private static readonly Vector2 ButtonSize = new(64, 64);
    private const string BindSeparator = "+";

    private readonly DragDropHelper<ActionButton> _dragHelper;
    private readonly Dictionary<EntProtoId, ActionButton> _floatingButtons = new();
    private ActionCustomizationData _data = new();
    private LayoutContainer? _floatingRoot;
    private ActionButton? _dragShadow;
    private EntProtoId? _capturingFor;

    /// <summary>
    /// Создаёт контроллер и помощник перетаскивания кнопок.
    /// </summary>
    public ActionCustomizationUIController()
    {
        _dragHelper = new DragDropHelper<ActionButton>(OnBeginDrag, OnContinueDrag, OnEndDrag);
    }

    /// <summary>
    /// Подписывается на загрузку экрана и читает сохранённые настройки действий.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;

        LoadData();
    }

    /// <summary>
    /// Обновляет перетаскивание кнопки действия.
    /// </summary>
    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _dragHelper.Update(args.DeltaSeconds);
    }

    /// <summary>
    /// Включает перехват клавиш и создаёт тень перетаскивания при входе в игру.
    /// </summary>
    public void OnStateEntered(GameplayState state)
    {
        _input.FirstChanceOnKeyEvent += OnFirstChanceKeyEvent;

        _dragShadow = new ActionButton(EntityManager)
        {
            Locked = true,
            MouseFilter = Control.MouseFilterMode.Ignore,
            Visible = false,
        };
        UIManager.PopupRoot.AddChild(_dragShadow);
    }

    /// <summary>
    /// Отключает перехват клавиш и сбрасывает перетаскивание и назначение бинда при выходе из игры.
    /// </summary>
    public void OnStateExited(GameplayState state)
    {
        _input.FirstChanceOnKeyEvent -= OnFirstChanceKeyEvent;

        _dragHelper.EndDrag();
        _capturingFor = null;

        _dragShadow?.Dispose();
        _dragShadow = null;
    }

    /// <summary>
    /// Подписывается на изменения списка действий.
    /// </summary>
    public void OnSystemLoaded(ActionsSystem system)
    {
        system.LinkActions += OnActionsLinked;
        system.UnlinkActions += ClearFloatingButtons;
        system.ActionsUpdated += RefreshFloatingButtons;
    }

    /// <summary>
    /// Отписывается от изменений списка действий.
    /// </summary>
    public void OnSystemUnloaded(ActionsSystem system)
    {
        system.LinkActions -= OnActionsLinked;
        system.UnlinkActions -= ClearFloatingButtons;
        system.ActionsUpdated -= RefreshFloatingButtons;
    }

    /// <summary>
    /// Проверяет, пришло ли нажатие от Shift + ЛКМ.
    /// </summary>
    public static bool IsShiftClick(BoundKeyFunction function)
    {
        return function == EngineKeyFunctions.TextCursorSelect || function == ContentKeyFunctions.ExamineEntity;
    }

    /// <summary>
    /// Обрабатывает Shift + ЛКМ по кнопке действия: если мышь сдвинулась, кнопка перетаскивается на экран,
    /// иначе после отпускания начинается назначение бинда.
    /// </summary>
    public void OnShiftPressed(GUIBoundKeyEventArgs args, ActionButton button)
    {
        args.Handle();

        if (button.Action == null)
            return;

        _dragHelper.MouseDown(button);
    }

    /// <summary>
    /// Обновляет подпись собственного бинда на кнопке действия.
    /// </summary>
    public void UpdateBindLabel(ActionButton button)
    {
        if (button.Action is not { } action || !TryGetPrototype(action, out var proto))
        {
            button.CustomBindLabel.Visible = false;
            return;
        }

        if (_capturingFor == proto)
        {
            button.CustomBindLabel.Text = Loc.GetString("aquila-action-bind-waiting");
            button.CustomBindLabel.Visible = true;
            return;
        }

        if (!_data.Actions.TryGetValue(proto, out var entry) || entry.Key == null)
        {
            button.CustomBindLabel.Visible = false;
            return;
        }

        button.CustomBindLabel.Text = GetBindString(entry);
        button.CustomBindLabel.Visible = true;
    }

    private void OnScreenLoad()
    {
        OnScreenUnload();

        if (UIManager.ActiveScreen is not { } screen)
            return;

        _floatingRoot = new LayoutContainer
        {
            MouseFilter = Control.MouseFilterMode.Ignore,
        };
        screen.AddChild(_floatingRoot);
        LayoutContainer.SetAnchorPreset(_floatingRoot, LayoutContainer.LayoutPreset.Wide);

        RefreshFloatingButtons();
    }

    private void OnScreenUnload()
    {
        ClearFloatingButtons();

        _floatingRoot?.Dispose();
        _floatingRoot = null;
    }

    private void OnActionsLinked(ActionsComponent component)
    {
        RefreshFloatingButtons();
    }

    private void RefreshFloatingButtons()
    {
        if (_floatingRoot == null || _actionsSystem == null)
            return;

        var shown = new HashSet<EntProtoId>();
        foreach (var action in _actionsSystem.GetClientActions())
        {
            if (!TryGetPrototype(action, out var proto) ||
                !_data.Actions.TryGetValue(proto, out var entry) ||
                entry.Position is not { } position ||
                !shown.Add(proto))
            {
                continue;
            }

            if (!_floatingButtons.TryGetValue(proto, out var button))
            {
                button = new ActionButton(EntityManager) { Locked = true };
                button.ActionPressed += OnFloatingPressed;
                button.ActionUnpressed += OnFloatingUnpressed;
                _floatingButtons.Add(proto, button);
                _floatingRoot.AddChild(button);
            }

            button.UpdateData(action, _actionsSystem);
            LayoutContainer.SetAnchorLeft(button, position.X);
            LayoutContainer.SetAnchorRight(button, position.X);
            LayoutContainer.SetAnchorTop(button, position.Y);
            LayoutContainer.SetAnchorBottom(button, position.Y);
        }

        foreach (var proto in _floatingButtons.Keys.Where(proto => !shown.Contains(proto)).ToList())
        {
            RemoveFloatingButton(proto);
        }
    }

    private void ClearFloatingButtons()
    {
        foreach (var proto in _floatingButtons.Keys.ToList())
        {
            RemoveFloatingButton(proto);
        }
    }

    private void RemoveFloatingButton(EntProtoId proto)
    {
        if (!_floatingButtons.Remove(proto, out var button))
            return;

        button.ActionPressed -= OnFloatingPressed;
        button.ActionUnpressed -= OnFloatingUnpressed;
        button.Dispose();
    }

    private void OnFloatingPressed(GUIBoundKeyEventArgs args, ActionButton button)
    {
        if (args.Function == EngineKeyFunctions.UIClick || args.Function == EngineKeyFunctions.UIRightClick)
            args.Handle();
    }

    private void OnFloatingUnpressed(GUIBoundKeyEventArgs args, ActionButton button)
    {
        if (button.Action is not { } action)
            return;

        if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            args.Handle();

            if (TryGetPrototype(action, out var proto))
                SetPosition(proto, null);

            return;
        }

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        args.Handle();
        UIManager.GetUIController<ActionUIController>().TriggerAction(action);
    }

    private void OnFirstChanceKeyEvent(KeyEventArgs keyEvent, KeyEventType type)
    {
        if (type == KeyEventType.Up)
        {
            if (keyEvent.Key == Keyboard.Key.MouseLeft && _dragHelper.Dragged != null)
                FinishShiftClick();

            return;
        }

        if (keyEvent.Handled)
            return;

        if (_capturingFor is { } proto)
        {
            CaptureKey(proto, keyEvent);
            return;
        }

        TryTriggerBind(keyEvent, type);
    }

    private void FinishShiftClick()
    {
        var dragging = _dragHelper.IsDragging;
        var button = _dragHelper.Dragged;
        _dragHelper.EndDrag();

        if (button?.Action is not { } action || !TryGetPrototype(action, out var proto))
            return;

        if (!dragging)
        {
            StartCapture(proto);
            return;
        }

        if (_floatingRoot == null || IsMouseOverWindow())
            return;

        var size = _floatingRoot.Size;
        if (size.X <= 0 || size.Y <= 0)
            return;

        var position = (UIManager.MousePositionScaled.Position - _floatingRoot.GlobalPosition - ButtonSize / 2) / size;
        var maxPosition = Vector2.Max(Vector2.One - ButtonSize / size, Vector2.Zero);
        SetPosition(proto, Vector2.Clamp(position, Vector2.Zero, maxPosition));
    }

    private bool IsMouseOverWindow()
    {
        for (var control = UIManager.MouseGetControl(_input.MouseScreenPosition); control != null; control = control.Parent)
        {
            if (control is BaseWindow)
                return true;
        }

        return false;
    }

    private void SetPosition(EntProtoId proto, Vector2? position)
    {
        GetOrCreateEntry(proto).Position = position;
        SaveData();
        RefreshFloatingButtons();
    }

    private void StartCapture(EntProtoId proto)
    {
        _capturingFor = proto;
        _popup?.PopupCursor(Loc.GetString("aquila-action-bind-start"));
        RefreshBindLabels();
    }

    private void StopCapture()
    {
        _capturingFor = null;
        RefreshBindLabels();
    }

    private void CaptureKey(EntProtoId proto, KeyEventArgs keyEvent)
    {
        switch (keyEvent.Key)
        {
            case Keyboard.Key.Shift:
            case Keyboard.Key.Control:
            case Keyboard.Key.Alt:
            case Keyboard.Key.LSystem:
            case Keyboard.Key.RSystem:
                return;
            case Keyboard.Key.MouseLeft:
            case Keyboard.Key.MouseRight:
                StopCapture();
                return;
            case Keyboard.Key.Escape:
                keyEvent.Handle();
                StopCapture();
                return;
            case Keyboard.Key.BackSpace:
            case Keyboard.Key.Delete:
                keyEvent.Handle();
                SetBind(proto, null);
                return;
        }

        keyEvent.Handle();
        SetBind(proto, keyEvent);
    }

    private void SetBind(EntProtoId proto, KeyEventArgs? keyEvent)
    {
        if (keyEvent != null)
        {
            foreach (var (otherProto, otherEntry) in _data.Actions)
            {
                if (otherProto != proto && IsBindPressed(otherEntry, keyEvent))
                    otherEntry.Key = null;
            }
        }

        var entry = GetOrCreateEntry(proto);
        entry.Key = keyEvent?.Key;
        entry.Shift = keyEvent?.Shift ?? false;
        entry.Control = keyEvent?.Control ?? false;
        entry.Alt = keyEvent?.Alt ?? false;

        var message = keyEvent == null
            ? Loc.GetString("aquila-action-bind-cleared")
            : Loc.GetString("aquila-action-bind-set", ("key", GetBindString(entry)));
        _popup?.PopupCursor(message);

        SaveData();
        StopCapture();
    }

    private void TryTriggerBind(KeyEventArgs keyEvent, KeyEventType type)
    {
        if (UIManager.KeyboardFocused != null || _actionsSystem == null)
            return;

        foreach (var action in _actionsSystem.GetClientActions())
        {
            if (!TryGetPrototype(action, out var proto) ||
                !_data.Actions.TryGetValue(proto, out var entry) ||
                !IsBindPressed(entry, keyEvent))
            {
                continue;
            }

            keyEvent.Handle();

            if (type == KeyEventType.Down)
                UIManager.GetUIController<ActionUIController>().TriggerAction(action);

            return;
        }
    }

    private void RefreshBindLabels()
    {
        if (UIManager.GetActiveUIWidgetOrNull<ActionsBar>() is { } actionsBar)
        {
            foreach (var button in actionsBar.ActionsContainer.GetButtons())
            {
                UpdateBindLabel(button);
            }
        }

        foreach (var button in _floatingButtons.Values)
        {
            UpdateBindLabel(button);
        }

        UIManager.GetUIController<ActionUIController>().QueueWindowUpdate();
    }

    private bool OnBeginDrag()
    {
        if (_dragShadow == null || _actionsSystem == null || _dragHelper.Dragged?.Action is not { } action)
            return false;

        _dragShadow.UpdateData(action, _actionsSystem);
        _dragShadow.Visible = true;
        MoveDragShadow();
        return true;
    }

    private bool OnContinueDrag(float frameTime)
    {
        MoveDragShadow();
        return true;
    }

    private void OnEndDrag()
    {
        if (_dragShadow == null)
            return;

        _dragShadow.Visible = false;
        _dragShadow.ClearData();
    }

    private void MoveDragShadow()
    {
        if (_dragShadow != null)
            LayoutContainer.SetPosition(_dragShadow, UIManager.MousePositionScaled.Position - ButtonSize / 2);
    }

    private bool TryGetPrototype(EntityUid action, out EntProtoId proto)
    {
        proto = default;

        if (!EntityManager.TryGetComponent(action, out MetaDataComponent? meta) || meta.EntityPrototype is not { } prototype)
            return false;

        proto = prototype.ID;
        return true;
    }

    private ActionCustomizationEntry GetOrCreateEntry(EntProtoId proto)
    {
        if (_data.Actions.TryGetValue(proto, out var entry))
            return entry;

        entry = new ActionCustomizationEntry();
        _data.Actions.Add(proto, entry);
        return entry;
    }

    private static bool IsBindPressed(ActionCustomizationEntry entry, KeyEventArgs keyEvent)
    {
        return entry.Key == keyEvent.Key &&
               entry.Shift == keyEvent.Shift &&
               entry.Control == keyEvent.Control &&
               entry.Alt == keyEvent.Alt;
    }

    private string GetBindString(ActionCustomizationEntry entry)
    {
        var parts = new List<string>();

        if (entry.Control)
            parts.Add(_input.GetKeyName(Keyboard.Key.Control));

        if (entry.Shift)
            parts.Add(_input.GetKeyName(Keyboard.Key.Shift));

        if (entry.Alt)
            parts.Add(_input.GetKeyName(Keyboard.Key.Alt));

        if (entry.Key is { } key)
            parts.Add(_input.GetKeyName(key));

        return string.Join(BindSeparator, parts);
    }

    private void LoadData()
    {
        if (!_resource.UserData.Exists(SavePath))
            return;

        try
        {
            using var reader = _resource.UserData.OpenText(SavePath);
            var document = DataNodeParser.ParseYamlStream(reader).First();
            _data = _serialization.Read<ActionCustomizationData>(document.Root, notNullableOverride: true);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load action customization: {e}");
        }
    }

    private void SaveData()
    {
        var emptyEntries = _data.Actions
            .Where(pair => pair.Value.Key == null && pair.Value.Position == null)
            .Select(pair => pair.Key)
            .ToList();

        foreach (var proto in emptyEntries)
        {
            _data.Actions.Remove(proto);
        }

        try
        {
            var node = _serialization.WriteValueAs<MappingDataNode>(_data);
            using var writer = _resource.UserData.OpenWriteText(SavePath);
            new YamlStream { new YamlDocument(node.ToYaml()) }.Save(writer, false);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save action customization: {e}");
        }
    }
}
