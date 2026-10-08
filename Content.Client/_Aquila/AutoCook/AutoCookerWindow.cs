using System.Linq;
using System.Numerics;
using Content.Client._Aquila.PDA;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared._Aquila.AutoCook;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Aquila.AutoCook;

public sealed class AutoCookerWindow : FancyWindow
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private static readonly int[] Amounts = [5, 10, 15, 30, 50];
    private const int MaxShownRecipes = 250;

    private static readonly Comparer<string> NameComparer =
        Comparer<string>.Create((a, b) => string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase));

    public event Action<AutoCookRecipeId, int>? OnStart;
    public event Action? OnCancel;
    public event Action? OnEject;
    public event Action<int>? OnRemoveQueued;
    public event Action? OnFlushBuffer;
    public event Action? OnFillBuffer;

    private readonly AutoCookText _text;

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
    private readonly AutoCookBar _outputBar = new() { MinSize = new Vector2(0, 8), BarColor = PdaStyle.Accent };
    private readonly Button _ejectButton = new() { Text = Loc.GetString("autocook-eject") };
    private readonly BoxContainer _stock = new() { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 2, HorizontalExpand = true };
    private readonly Label _stockTitle = Header(string.Empty);
    private readonly Button _fillButton = new() { Text = Loc.GetString("autocook-buffer-fill") };
    private readonly Button _flushButton = new() { Text = Loc.GetString("autocook-buffer-flush") };
    private readonly PanelContainer _jobPanel;
    private readonly PanelContainer _outputPanel;
    private readonly PanelContainer _stockPanel;
    private readonly PanelContainer _queuePanel;
    private readonly OptionButton _category = new() { HorizontalExpand = true };
    private readonly CheckBox _availableOnly = new() { Text = Loc.GetString("autocook-filter-available") };
    private readonly Label _queueTitle = new();
    private readonly BoxContainer _queueList = new() { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 2, HorizontalExpand = true };
    private readonly List<string> _groups = new();

    private readonly List<RecipeView> _recipes = new();

    private AutoCookerBoundUserInterfaceState? _state;
    private AutoCookRecipeId? _selected;
    private int _amount = 10;
    private string? _groupFilter;
    private Control? _activeStepLabel;
    private bool _scrollPending;

    private readonly record struct RecipeView(AutoCookRecipeEntry Entry, string Name);

    public AutoCookerWindow()
    {
        IoCManager.InjectDependencies(this);
        _text = new AutoCookText(_proto);

        Resizable = false;
        MinSize = new Vector2(940, 660);
        SetSize = new Vector2(940, 660);

        _status.FontColorOverride = PdaStyle.Red;
        _jobHint.FontColorOverride = PdaStyle.TextMuted;
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
            if (_selected is { } selected)
                OnStart?.Invoke(selected, _amount);
        };
        _cancelButton.OnPressed += _ => OnCancel?.Invoke();
        _ejectButton.OnPressed += _ => OnEject?.Invoke();
        _flushButton.OnPressed += _ => OnFlushBuffer?.Invoke();
        _fillButton.OnPressed += _ => OnFillBuffer?.Invoke();

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

        BuildAmountButtons();
    }

    public void UpdateState(AutoCookerBoundUserInterfaceState state)
    {
        _state = state;

        Title = state.Kind switch
        {
            AutoCookKind.Bar => Loc.GetString("autocook-title-bar"),
            AutoCookKind.Kitchen => Loc.GetString("autocook-title-kitchen"),
            _ => Loc.GetString("autocook-title-chem"),
        };

        var synthesis = state.Kind != AutoCookKind.Kitchen;
        _amountRow.Visible = synthesis;
        _outputPanel.Visible = synthesis;
        _stockPanel.Visible = !synthesis || state.HasBuffer;
        _stockTitle.Text = Loc.GetString(synthesis ? "autocook-buffer" : "autocook-stock");
        _flushButton.Visible = synthesis;
        _fillButton.Visible = synthesis;
        _fillButton.Disabled = state.Output is not { } beaker || beaker.Volume <= 0;
        _flushButton.Disabled = state.Stock.Count == 0 || state.Output == null;

        _recipes.Clear();
        _recipes.AddRange(state.Recipes
            .Select(entry => new RecipeView(entry, _text.ResultName(entry.Id.Kind, entry.Result)))
            .OrderByDescending(view => view.Entry.Available)
            .ThenBy(view => view.Name, NameComparer));

        if (_selected is { } selected && _recipes.All(view => view.Entry.Id != selected))
            _selected = null;

        RebuildCategories();
        RebuildRecipes();
        RebuildDetails();
        RebuildJob();
        RebuildOutput();
        RebuildStock();
        RebuildQueue();
        UpdateStartButton();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_scrollPending && _activeStepLabel is { Height: > 0f })
        {
            _scrollPending = false;
            _jobScroll.SetScrollValue(new Vector2(0, MathF.Max(0f, _activeStepLabel.Position.Y - 24f)));
        }

        var job = _state?.Job;
        if (job == null || job.ActiveIndex >= job.Steps.Count)
        {
            _stepBar.Value = job != null ? 1f : 0f;
            _totalBar.Value = job != null ? 1f : 0f;
            return;
        }

        var duration = Math.Max(0.01, job.Steps[job.ActiveIndex].Duration.TotalSeconds);
        var elapsed = (job.PausedElapsed ?? _timing.CurTime - job.StepStart).TotalSeconds;
        elapsed = Math.Clamp(elapsed, 0, duration);
        _stepBar.Value = (float) (elapsed / duration);

        var total = 0.0;
        var done = 0.0;
        for (var i = 0; i < job.Steps.Count; i++)
        {
            total += job.Steps[i].Duration.TotalSeconds;
            if (i < job.ActiveIndex)
                done += job.Steps[i].Duration.TotalSeconds;
        }

        _totalBar.Value = total > 0 ? (float) ((done + elapsed) / total) : 0f;
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

        _stockTitle.HorizontalExpand = true;
        var head = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 8 };
        head.AddChild(_stockTitle);
        head.AddChild(_fillButton);
        head.AddChild(_flushButton);

        box.AddChild(head);
        box.AddChild(scroll);
        return box;
    }

    private void BuildAmountButtons()
    {
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

            button.OnPressed += _ => _amount = amount;
            _amountRow.AddChild(button);
        }
    }

    private void RebuildCategories()
    {
        _groups.Clear();
        _groups.AddRange(_recipes
            .Select(view => view.Entry.Group)
            .Distinct()
            .OrderBy(group => Loc.GetString(group), NameComparer));

        _category.Clear();
        _category.AddItem(Loc.GetString("autocook-filter-all"), 0);

        for (var i = 0; i < _groups.Count; i++)
        {
            _category.AddItem(Loc.GetString(_groups[i]), i + 1);
        }

        var selected = _groupFilter == null ? -1 : _groups.IndexOf(_groupFilter);
        if (selected < 0)
            _groupFilter = null;

        _category.SelectId(selected + 1);
    }

    private void RebuildRecipes()
    {
        _recipeList.RemoveAllChildren();

        var recipeGroup = new ButtonGroup(false);
        var filter = _search.Text.Trim();
        var shown = 0;

        foreach (var (entry, name) in _recipes)
        {
            if ((_groupFilter != null && entry.Group != _groupFilter)
                || (_availableOnly.Pressed && !entry.Available)
                || (filter.Length > 0 && !name.Contains(filter, StringComparison.CurrentCultureIgnoreCase)))
                continue;

            if (++shown > MaxShownRecipes)
                break;

            var button = new Button
            {
                Text = name,
                ToggleMode = true,
                Group = recipeGroup,
                ClipText = true,
                HorizontalExpand = true,
                Modulate = entry.Available ? Color.White : Color.White.WithAlpha(0.45f),
            };

            if (entry.Id == _selected)
                button.Pressed = true;

            var id = entry.Id;
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

        if (FindSelected() is not { } selected)
        {
            SetText(_title, Loc.GetString("autocook-no-recipe"), PdaStyle.TextHeader);
            return;
        }

        var (entry, name) = selected;

        SetText(_title, name, PdaStyle.TextHeader);

        foreach (var ingredient in entry.Ingredients)
        {
            _ingredients.AddChild(Wrapped(_text.IngredientText(ingredient), ingredient.Ok ? PdaStyle.TextNormal : PdaStyle.Red));
        }

        for (var i = 0; i < entry.Steps.Count; i++)
        {
            _steps.AddChild(Wrapped($"{i + 1}. {_text.StepText(entry.Steps[i])}", PdaStyle.TextInteractive));
        }
    }

    private void RebuildJob()
    {
        _jobSteps.RemoveAllChildren();
        _activeStepLabel = null;

        var job = _state?.Job;
        _jobPanel.Visible = job != null;

        if (job == null)
            return;

        SetText(_jobTitle, $"{Loc.GetString("autocook-job")}: {_text.ResultName(job.Kind, job.Result)}", PdaStyle.TextHeader);

        for (var i = 0; i < job.Steps.Count; i++)
        {
            var (mark, color) = i < job.ActiveIndex
                ? ("✓", PdaStyle.Accent)
                : i == job.ActiveIndex
                    ? ("▶", PdaStyle.Yellow)
                    : ("·", PdaStyle.TextMuted);

            var label = Wrapped($"{mark} {_text.StepText(job.Steps[i])}", color);
            _jobSteps.AddChild(label);

            if (i == job.ActiveIndex)
                _activeStepLabel = label;
        }

        _scrollPending = true;

        _jobHint.Text = job.PausedElapsed != null
            ? Loc.GetString("autocook-paused")
            : job.WaitingOutput ? Loc.GetString("autocook-waiting-output") : string.Empty;
    }

    private void RebuildOutput()
    {
        if (_state?.Output is not { } output)
        {
            SetText(_outputName, Loc.GetString("autocook-no-container"), PdaStyle.TextMuted);
            _outputBar.Value = 0f;
            _ejectButton.Disabled = true;
            return;
        }

        SetText(_outputName, $"{output.Name}  {output.Volume}/{output.MaxVolume}", PdaStyle.TextNormal);
        _outputBar.BarColor = output.Color;
        _outputBar.Value = output.MaxVolume > 0 ? (output.Volume / output.MaxVolume).Float() : 0f;
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

        foreach (var text in _state.Stock.Select(_text.StockText).OrderBy(text => text, NameComparer))
        {
            _stock.AddChild(Wrapped(text, PdaStyle.TextNormal));
        }
    }

    private void RebuildQueue()
    {
        _queueList.RemoveAllChildren();

        var queue = _state?.Queue ?? [];
        _queuePanel.Visible = _state?.Job != null || queue.Count > 0;
        _queueTitle.Text = $"{Loc.GetString("autocook-queue")} {queue.Count}/{_state?.MaxQueue ?? 0}";

        if (queue.Count == 0)
        {
            _queueList.AddChild(Wrapped(Loc.GetString("autocook-queue-empty"), PdaStyle.TextMuted));
            return;
        }

        for (var i = 0; i < queue.Count; i++)
        {
            var order = queue[i];
            var amount = _state?.Kind != AutoCookKind.Kitchen ? $" ×{order.Amount}" : string.Empty;

            var row = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 6 };
            row.AddChild(Wrapped($"{i + 1}. {_text.ResultName(order.Kind, order.Result)}{amount}", PdaStyle.TextNormal));

            var index = i;
            var remove = new Button { Text = "✕", MinWidth = 28 };
            remove.OnPressed += _ => OnRemoveQueued?.Invoke(index);
            row.AddChild(remove);

            _queueList.AddChild(row);
        }
    }

    private void UpdateStartButton()
    {
        var powered = _state?.Powered ?? false;
        var busy = _state is { } state && (state.Job != null || state.Queue.Count > 0);
        var queueFull = busy && _state!.Queue.Count >= _state.MaxQueue;
        var needsOutput = _state?.Kind != AutoCookKind.Kitchen && _state?.Output == null;

        _startButton.Text = Loc.GetString(busy ? "autocook-queue-add" : "autocook-start");
        _startButton.Disabled = FindSelected() is not { Entry.Available: true } || !powered || queueFull || needsOutput;
        _status.Text = powered ? string.Empty : Loc.GetString("autocook-no-power");
    }

    private RecipeView? FindSelected()
    {
        foreach (var view in _recipes)
        {
            if (view.Entry.Id == _selected)
                return view;
        }

        return null;
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
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = background,
                BorderColor = PdaStyle.Separator,
                BorderThickness = new Thickness(1),
            },
        };

        panel.AddChild(child);
        return panel;
    }
}
