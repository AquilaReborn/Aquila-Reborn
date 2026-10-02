using System.Linq;
using System.Numerics;
using Content.Client._Aquila.PDA;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared._Aquila.AutoCook;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Aquila.AutoCook;

public sealed class AutoCookerWindow : FancyWindow
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly int[] Amounts = [5, 10, 15, 30, 50];

    public event Action<string, int>? OnStart;
    public event Action? OnCancel;
    public event Action? OnEject;
    public event Action<int>? OnRemoveQueued;

    private readonly BoxContainer _recipeList = new() { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 3, HorizontalExpand = true };
    private readonly LineEdit _search = new() { PlaceHolder = Loc.GetString("autocook-search"), HorizontalExpand = true };
    private readonly RichTextLabel _title = new() { HorizontalExpand = true };
    private readonly Label _status = new();
    private readonly BoxContainer _ingredients = new() { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 3, HorizontalExpand = true };
    private readonly BoxContainer _steps = new() { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 3, HorizontalExpand = true };
    private readonly BoxContainer _amountRow = new() { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 4 };
    private readonly Button _startButton = new() { Text = Loc.GetString("autocook-start"), HorizontalExpand = true };
    private readonly RichTextLabel _jobTitle = new() { HorizontalExpand = true };
    private readonly BoxContainer _jobSteps = new() { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 2, HorizontalExpand = true };
    private readonly ScrollContainer _jobScroll = new() { HScrollEnabled = false, MinHeight = 70, MaxHeight = 140 };
    private readonly AutoCookBar _stepBar = new() { MinSize = new Vector2(0, 10), BarColor = PdaStyle.Accent };
    private readonly AutoCookBar _totalBar = new() { MinSize = new Vector2(0, 6), BarColor = PdaStyle.Yellow };
    private readonly Button _cancelButton = new() { Text = Loc.GetString("autocook-cancel") };
    private readonly Label _jobHint = new();
    private readonly RichTextLabel _outputName = new() { HorizontalExpand = true };
    private readonly AutoCookBar _outputBar = new() { MinSize = new Vector2(0, 8) };
    private readonly Button _ejectButton = new() { Text = Loc.GetString("autocook-eject") };
    private readonly BoxContainer _stock = new() { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 2, HorizontalExpand = true };
    private readonly PanelContainer _jobPanel;
    private readonly PanelContainer _outputPanel;
    private readonly PanelContainer _stockPanel;
    private readonly PanelContainer _queuePanel;
    private readonly OptionButton _category = new() { HorizontalExpand = true };
    private readonly CheckBox _availableOnly = new() { Text = Loc.GetString("autocook-filter-available") };
    private readonly Label _queueTitle = new();
    private readonly BoxContainer _queueList = new() { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 2, HorizontalExpand = true };
    private readonly List<string> _groups = new();

    private ButtonGroup _recipeGroup = new(false);
    private AutoCookerBoundUserInterfaceState? _state;
    private string? _selected;
    private int _amount = 10;
    private string? _groupFilter;
    private AutoCookJobInfo? _job;
    private Control? _activeStepLabel;
    private bool _scrollPending;

    public AutoCookerWindow()
    {
        IoCManager.InjectDependencies(this);

        Resizable = false;
        MinSize = new Vector2(940, 660);
        SetSize = new Vector2(940, 660);

        _status.FontColorOverride = PdaStyle.Red;
        _jobHint.FontColorOverride = PdaStyle.TextMuted;
        _outputBar.BarColor = PdaStyle.Accent;
        SetText(_title, Loc.GetString("autocook-no-recipe"), PdaStyle.TextHeader);

        _search.OnTextChanged += _ => RebuildRecipes();
        _availableOnly.OnToggled += _ => RebuildRecipes();
        _category.OnItemSelected += args =>
        {
            _category.SelectId(args.Id);
            _groupFilter = args.Id > 0 && args.Id <= _groups.Count ? _groups[args.Id - 1] : null;
            RebuildRecipes();
        };
        _startButton.OnPressed += _ =>
        {
            if (_selected != null)
                OnStart?.Invoke(_selected, _amount);
        };
        _cancelButton.OnPressed += _ => OnCancel?.Invoke();
        _ejectButton.OnPressed += _ => OnEject?.Invoke();

        _jobPanel = Panel(BuildJobPanel(), PdaStyle.BackgroundTertiary);
        _queuePanel = Panel(BuildQueuePanel(), PdaStyle.BackgroundTertiary);
        _outputPanel = Panel(BuildOutputPanel(), PdaStyle.BackgroundTertiary);
        _stockPanel = Panel(BuildStockPanel(), PdaStyle.BackgroundTertiary);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            Margin = new Thickness(8),
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        root.AddChild(BuildLeft());
        root.AddChild(BuildRight());
        ContentsContainer.AddChild(Panel(root, PdaStyle.Background));
    }

    private Control BuildLeft()
    {
        var list = new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
        };
        list.AddChild(_recipeList);

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
            Margin = new Thickness(8),
        };

        box.AddChild(Header(Loc.GetString("autocook-recipes")));
        box.AddChild(_search);
        box.AddChild(_category);
        box.AddChild(_availableOnly);
        box.AddChild(list);

        var panel = Panel(box, PdaStyle.BackgroundSecondary);
        panel.HorizontalExpand = false;
        panel.SetWidth = 280;
        panel.VerticalExpand = true;
        return panel;
    }

    private Control BuildRight()
    {
        var right = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        var top = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 8 };
        top.AddChild(_title);
        top.AddChild(_status);

        var columns = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 16,
            Margin = new Thickness(10),
            HorizontalExpand = true,
        };

        var ingredientsColumn = Column(Loc.GetString("autocook-ingredients"), _ingredients);
        ingredientsColumn.SizeFlagsStretchRatio = 2f;
        var stepsColumn = Column(Loc.GetString("autocook-steps"), _steps);
        stepsColumn.SizeFlagsStretchRatio = 3f;
        columns.AddChild(ingredientsColumn);
        columns.AddChild(new PanelContainer
        {
            MinWidth = 1,
            PanelOverride = new StyleBoxFlat { BackgroundColor = PdaStyle.Separator },
        });
        columns.AddChild(stepsColumn);

        var detailsScroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            HScrollEnabled = false,
            MinHeight = 120,
        };
        detailsScroll.AddChild(columns);

        var details = Panel(detailsScroll, PdaStyle.BackgroundTertiary);
        details.VerticalExpand = true;

        var controls = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };
        controls.AddChild(_amountRow);
        controls.AddChild(_startButton);

        right.AddChild(top);
        right.AddChild(details);
        right.AddChild(controls);
        right.AddChild(_queuePanel);
        right.AddChild(_jobPanel);
        right.AddChild(_outputPanel);
        right.AddChild(_stockPanel);

        return right;
    }

    private Control BuildJobPanel()
    {
        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
            Margin = new Thickness(10),
        };

        var head = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 8 };
        head.AddChild(_jobTitle);
        head.AddChild(_cancelButton);

        _jobScroll.AddChild(_jobSteps);

        box.AddChild(head);
        box.AddChild(_jobScroll);
        box.AddChild(_stepBar);
        box.AddChild(_totalBar);
        box.AddChild(_jobHint);
        return box;
    }

    private Control BuildOutputPanel()
    {
        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
            Margin = new Thickness(10),
        };

        var head = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 8 };
        head.AddChild(_outputName);
        head.AddChild(_ejectButton);

        box.AddChild(head);
        box.AddChild(_outputBar);
        return box;
    }

    private Control BuildStockPanel()
    {
        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
            Margin = new Thickness(10),
        };

        var scroll = new ScrollContainer { MinHeight = 70, MaxHeight = 110, HScrollEnabled = false };
        scroll.AddChild(_stock);

        box.AddChild(Header(Loc.GetString("autocook-stock")));
        box.AddChild(scroll);
        return box;
    }

    public void UpdateState(AutoCookerBoundUserInterfaceState state)
    {
        _state = state;
        _job = state.Job;

        Title = state.Kind switch
        {
            AutoCookKind.Bar => Loc.GetString("autocook-title-bar"),
            AutoCookKind.Kitchen => Loc.GetString("autocook-title-kitchen"),
            _ => Loc.GetString("autocook-title-chem"),
        };

        var showAmount = state.Kind != AutoCookKind.Kitchen;
        _amountRow.Visible = showAmount;
        _outputPanel.Visible = showAmount;
        _stockPanel.Visible = !showAmount;

        BuildAmountButtons();

        RebuildCategories();

        if (_selected != null && state.Recipes.All(r => r.Id != _selected))
            _selected = null;

        RebuildRecipes();
        RebuildDetails();
        RebuildJob();
        RebuildOutput();
        RebuildStock();
        RebuildQueue();
        UpdateStartButton();
    }

    private void BuildAmountButtons()
    {
        _amountRow.RemoveAllChildren();

        var group = new ButtonGroup();
        foreach (var amount in Amounts)
        {
            var button = new Button
            {
                Text = amount.ToString(),
                ToggleMode = true,
                Group = group,
                MinWidth = 42,
            };

            if (amount == _amount)
                button.Pressed = true;

            var value = amount;
            button.OnPressed += _ => _amount = value;
            _amountRow.AddChild(button);
        }
    }

    private void RebuildRecipes()
    {
        _recipeList.RemoveAllChildren();
        _recipeGroup = new ButtonGroup(false);

        if (_state == null)
            return;

        var filter = _search.Text.Trim();
        var shown = 0;

        foreach (var recipe in _state.Recipes)
        {
            if ((_groupFilter != null && recipe.Group != _groupFilter)
                || (_availableOnly.Pressed && !recipe.Available)
                || (filter.Length > 0 && !recipe.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase)))
                continue;

            if (++shown > 250)
                break;

            var button = new Button
            {
                Text = recipe.Name,
                ToggleMode = true,
                Group = _recipeGroup,
                ClipText = true,
                HorizontalExpand = true,
                Modulate = recipe.Available ? Color.White : new Color(1f, 1f, 1f, 0.45f),
            };

            if (recipe.Id == _selected)
                button.Pressed = true;

            var id = recipe.Id;
            button.OnPressed += _ =>
            {
                _selected = id;
                RebuildDetails();
                UpdateStartButton();
            };

            _recipeList.AddChild(button);
        }
    }

    private void RebuildDetails()
    {
        _ingredients.RemoveAllChildren();
        _steps.RemoveAllChildren();

        var recipe = _state?.Recipes.Find(r => r.Id == _selected);
        if (recipe == null)
        {
            SetText(_title, Loc.GetString("autocook-no-recipe"), PdaStyle.TextHeader);
            return;
        }

        SetText(_title, recipe.Name, PdaStyle.TextHeader);

        foreach (var ingredient in recipe.Ingredients)
        {
            var text = string.IsNullOrEmpty(ingredient.Amount)
                ? ingredient.Label
                : $"{ingredient.Label} — {ingredient.Amount}";

            _ingredients.AddChild(Wrapped(text, ingredient.Ok ? PdaStyle.TextNormal : PdaStyle.Red));
        }

        var index = 1;
        foreach (var step in recipe.Steps)
        {
            _steps.AddChild(Wrapped($"{index++}. {step}", PdaStyle.TextInteractive));
        }
    }

    private void RebuildJob()
    {
        _jobSteps.RemoveAllChildren();
        _jobPanel.Visible = _job != null;
        _activeStepLabel = null;

        if (_job == null)
            return;

        SetText(_jobTitle, $"{Loc.GetString("autocook-job")}: {_job.RecipeName}", PdaStyle.TextHeader);

        foreach (var step in _job.Steps)
        {
            var (mark, color) = step.Status switch
            {
                AutoCookStepStatus.Done => ("✓", PdaStyle.Accent),
                AutoCookStepStatus.Active => ("▶", PdaStyle.Yellow),
                _ => ("·", PdaStyle.TextMuted),
            };

            var label = Wrapped($"{mark} {step.Text}", color);
            _jobSteps.AddChild(label);

            if (step.Status == AutoCookStepStatus.Active)
                _activeStepLabel = label;
        }

        _scrollPending = true;

        _jobHint.Text = _job.Paused
            ? Loc.GetString("autocook-paused")
            : _job.WaitingOutput ? Loc.GetString("autocook-waiting-output") : string.Empty;
    }

    private void RebuildOutput()
    {
        var output = _state?.Output;
        if (output == null)
        {
            SetText(_outputName, Loc.GetString("autocook-no-container"), PdaStyle.TextMuted);
            _outputBar.Value = 0f;
            _ejectButton.Disabled = true;
            return;
        }

        SetText(_outputName, $"{output.Name}  {output.Volume}/{output.MaxVolume}", PdaStyle.TextNormal);
        _outputBar.BarColor = output.Color;
        _outputBar.Value = output.MaxVolume > 0 ? (float) (output.Volume / output.MaxVolume) : 0f;
        _ejectButton.Disabled = false;
    }

    private void RebuildStock()
    {
        _stock.RemoveAllChildren();

        if (_state == null || _state.Stock.Count == 0)
        {
            _stock.AddChild(Wrapped(Loc.GetString("autocook-stock-empty"), PdaStyle.TextMuted));
            return;
        }

        foreach (var entry in _state.Stock)
        {
            _stock.AddChild(Wrapped($"{entry.Name} — {entry.Amount}", PdaStyle.TextNormal));
        }
    }

    private void UpdateStartButton()
    {
        var recipe = _state?.Recipes.Find(r => r.Id == _selected);
        var powered = _state?.Powered ?? false;
        var busy = _job != null || _state?.Queue.Count > 0;
        var queueFull = busy && _state != null && _state.Queue.Count >= _state.MaxQueue;

        _startButton.Text = Loc.GetString(busy ? "autocook-queue-add" : "autocook-start");
        _startButton.Disabled = recipe == null || !recipe.Available || !powered || queueFull
            || (_state?.Kind != AutoCookKind.Kitchen && _state?.Output == null);

        _status.Text = powered ? string.Empty : Loc.GetString("autocook-no-power");
    }

    private Control BuildQueuePanel()
    {
        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
            Margin = new Thickness(10),
        };

        var scroll = new ScrollContainer { MinHeight = 40, MaxHeight = 90, HScrollEnabled = false };
        scroll.AddChild(_queueList);

        _queueTitle.FontColorOverride = PdaStyle.TextHeader;
        box.AddChild(_queueTitle);
        box.AddChild(scroll);
        return box;
    }

    private void RebuildQueue()
    {
        _queueList.RemoveAllChildren();

        var queue = _state?.Queue ?? [];
        _queuePanel.Visible = _job != null || queue.Count > 0;
        _queueTitle.Text = $"{Loc.GetString("autocook-queue")} {queue.Count}/{_state?.MaxQueue ?? 0}";

        if (queue.Count == 0)
        {
            _queueList.AddChild(Wrapped(Loc.GetString("autocook-queue-empty"), PdaStyle.TextMuted));
            return;
        }

        for (var i = 0; i < queue.Count; i++)
        {
            var row = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 6 };
            var amount = _state?.Kind != AutoCookKind.Kitchen ? $" ×{queue[i].Amount}" : string.Empty;
            row.AddChild(Wrapped($"{i + 1}. {queue[i].Name}{amount}", PdaStyle.TextNormal));

            var index = i;
            var remove = new Button { Text = "✕", MinWidth = 28 };
            remove.OnPressed += _ => OnRemoveQueued?.Invoke(index);
            row.AddChild(remove);

            _queueList.AddChild(row);
        }
    }

    private void RebuildCategories()
    {
        _groups.Clear();
        _category.Clear();
        _category.AddItem(Loc.GetString("autocook-filter-all"), 0);

        if (_state == null)
            return;

        _groups.AddRange(_state.Recipes
            .Select(recipe => recipe.Group)
            .Where(group => !string.IsNullOrEmpty(group))
            .Distinct()
            .OrderBy(group => group));

        for (var i = 0; i < _groups.Count; i++)
        {
            _category.AddItem(_groups[i], i + 1);
        }

        var selected = _groupFilter == null ? -1 : _groups.IndexOf(_groupFilter);
        if (selected < 0)
            _groupFilter = null;

        _category.SelectId(selected < 0 ? 0 : selected + 1);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_scrollPending && _activeStepLabel is { Height: > 0f })
        {
            _scrollPending = false;
            _jobScroll.SetScrollValue(new Vector2(0, MathF.Max(0f, _activeStepLabel.Position.Y - 24f)));
        }

        if (_job == null || _job.ActiveIndex >= _job.Steps.Count)
        {
            _stepBar.Value = _job != null ? 1f : 0f;
            _totalBar.Value = _job != null ? 1f : 0f;
            return;
        }

        var duration = MathF.Max(0.01f, _job.Steps[_job.ActiveIndex].Duration);
        var elapsed = _job.Paused
            ? (float) _job.PausedElapsed.TotalSeconds
            : (float) (_timing.CurTime - _job.StepStart).TotalSeconds;

        elapsed = Math.Clamp(elapsed, 0f, duration);
        _stepBar.Value = elapsed / duration;

        var total = 0f;
        var done = 0f;
        for (var i = 0; i < _job.Steps.Count; i++)
        {
            total += _job.Steps[i].Duration;
            if (i < _job.ActiveIndex)
                done += _job.Steps[i].Duration;
        }

        _totalBar.Value = total > 0f ? (done + elapsed) / total : 0f;
    }

    private static Control Column(string title, Control content)
    {
        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
            HorizontalExpand = true,
        };

        box.AddChild(Header(title));
        box.AddChild(content);
        return box;
    }

    private static Label Header(string text)
    {
        var label = new Label { Text = text, FontColorOverride = PdaStyle.TextHeader };
        label.StyleClasses.Add(StyleClass.LabelHeading);
        return label;
    }

    private static RichTextLabel Wrapped(string text, Color color)
    {
        var label = new RichTextLabel { HorizontalExpand = true };
        SetText(label, text, color);
        return label;
    }

    private static void SetText(RichTextLabel label, string text, Color color)
    {
        var message = new FormattedMessage();
        message.PushColor(color);
        message.AddText(text);
        message.Pop();
        label.SetMessage(message);
    }

    private static PanelContainer Panel(Control child, Color background)
    {
        var panel = new PanelContainer
        {
            HorizontalExpand = true,
            PanelOverride = Flat(background),
        };

        panel.AddChild(child);
        return panel;
    }

    private static StyleBoxFlat Flat(Color background)
    {
        return new StyleBoxFlat
        {
            BackgroundColor = background,
            BorderColor = PdaStyle.Separator,
            BorderThickness = new Thickness(1),
        };
    }
}

public sealed class AutoCookBar : Control
{
    public float Value { get; set; }
    public Color BarColor { get; set; } = PdaStyle.Accent;

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = PixelSize;
        handle.DrawRect(new UIBox2(0, 0, size.X, size.Y), PdaStyle.BackgroundFloating);

        var width = size.X * Math.Clamp(Value, 0f, 1f);
        if (width > 0f)
            handle.DrawRect(new UIBox2(0, 0, width, size.Y), BarColor);
    }
}
