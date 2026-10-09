using Content.Goobstation.Shared.Factory;
using Content.Server.Power.Components;
using Content.Server.Stack;
using Content.Shared._Aquila.AutoCook;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Storage.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.AutoCook;

public sealed partial class AutoCookerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly StartableMachineSystem _startable = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoCookerComponent, MapInitEvent>(OnMapInit, before: [typeof(ItemSlotsSystem)]);
        SubscribeLocalEvent<AutoCookerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AutoCookerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<AutoCookerComponent, BoundUIOpenedEvent>(OnContentsChanged);
        SubscribeLocalEvent<AutoCookerComponent, BoundUIClosedEvent>(OnUiClosed);
        SubscribeLocalEvent<AutoCookerComponent, EntInsertedIntoContainerMessage>(OnContentsChanged);
        SubscribeLocalEvent<AutoCookerComponent, EntRemovedFromContainerMessage>(OnContentsChanged);
        SubscribeLocalEvent<AutoCookerComponent, SolutionContainerChangedEvent>(OnContentsChanged);
        SubscribeLocalEvent<FitsInDispenserComponent, SolutionContainerChangedEvent>(OnBeakerSolutionChanged);
        SubscribeLocalEvent<AutoCookerComponent, AutoCookerStartMessage>(OnStartMessage);
        SubscribeLocalEvent<AutoCookerComponent, AutoCookerCancelMessage>(OnCancelMessage);
        SubscribeLocalEvent<AutoCookerComponent, AutoCookerRemoveQueuedMessage>(OnRemoveQueuedMessage);
        SubscribeLocalEvent<AutoCookerComponent, AutoCookerFlushBufferMessage>(OnFlushBufferMessage);
        SubscribeLocalEvent<AutoCookerComponent, AutoCookerFillBufferMessage>(OnFillBufferMessage);
        SubscribeLocalEvent<AutoCookerComponent, MachineStartedEvent>(OnMachineStarted);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<AutoCookerComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            var ent = (uid, comp);

            FlushUi(ent, now);

            if (comp.DoneUntil is { } doneUntil && now >= doneUntil)
            {
                comp.DoneUntil = null;
                UpdateVisuals(ent);
            }

            if (comp.Job is not { } job)
            {
                if (comp.Queue.Count > 0)
                    TryStartQueued(ent);

                continue;
            }

            if (!_power.IsPowered(uid))
            {
                Pause(ent, job, now);
                continue;
            }

            Resume(ent, job, now);

            if (!job.Finished && now - job.StepStart >= job.Steps[job.StepIndex].Data.Duration)
                AdvanceStep(ent, job, now);

            if (job.Finished)
                TryFinish(ent, job);
        }
    }

    private void OnMapInit(Entity<AutoCookerComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Kind != AutoCookKind.Kitchen)
            _itemSlots.AddItemSlot(ent.Owner, AutoCookerComponent.BeakerSlotId, ent.Comp.BeakerSlot);
        else
            _container.EnsureContainer<Container>(ent, ent.Comp.ProcessingContainer);

        UpdateVisuals(ent);
    }

    private void OnShutdown(Entity<AutoCookerComponent> ent, ref ComponentShutdown args)
    {
        _bufferEntries.Remove(ent.Owner);
        _kitchenEntries.Remove(ent.Owner);

        if (ent.Comp.Kind != AutoCookKind.Kitchen)
            _itemSlots.RemoveItemSlot(ent.Owner, ent.Comp.BeakerSlot);
    }

    private void OnPowerChanged(Entity<AutoCookerComponent> ent, ref PowerChangedEvent args)
    {
        UpdateVisuals(ent);
        UpdateUi(ent);
    }

    private void OnContentsChanged<T>(Entity<AutoCookerComponent> ent, ref T args)
    {
        UpdateUi(ent);
    }

    /// <summary>
    /// Изменения ёмкости в слоте (например, насос выкачал реагент) не доходят до машины сами, пробрасываем их в окно.
    /// </summary>
    private void OnBeakerSolutionChanged(Entity<FitsInDispenserComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (!_container.TryGetContainingContainer(ent.Owner, out var container)
            || container.ID != AutoCookerComponent.BeakerSlotId
            || !TryComp<AutoCookerComponent>(container.Owner, out var cooker))
            return;

        UpdateUi((container.Owner, cooker));
    }

    private void OnStartMessage(Entity<AutoCookerComponent> ent, ref AutoCookerStartMessage args)
    {
        if (!IsValidRecipe(ent.Comp, args.Recipe) || args.Amount < 1 || args.Amount > ent.Comp.MaxOrderAmount)
            return;

        var order = new AutoCookOrder(args.Recipe, args.Amount);
        ent.Comp.LastOrder = order;
        TryOrder(ent, order);
    }

    private bool IsValidRecipe(AutoCookerComponent comp, AutoCookRecipeId recipe)
    {
        if (string.IsNullOrEmpty(recipe.Id))
            return false;

        EnsureIndex();

        if (comp.Kind != AutoCookKind.Kitchen)
        {
            return recipe.Kind == AutoCookRecipeKind.Reagent
                   && _proto.TryIndex<ReagentPrototype>(recipe.Id, out var reagent)
                   && IsListedReagent(comp, reagent);
        }

        return recipe.Kind switch
        {
            AutoCookRecipeKind.Meal => TryGetMeal(comp, recipe.Id, out _),
            AutoCookRecipeKind.Mix => TryGetMix(recipe.Id, out _),
            AutoCookRecipeKind.Make => _transformSources.ContainsKey(recipe.Id),
            _ => false,
        };
    }

    private void OnCancelMessage(Entity<AutoCookerComponent> ent, ref AutoCookerCancelMessage args)
    {
        if (ent.Comp.Job is not { } job)
            return;

        if (ent.Comp.Kind == AutoCookKind.Kitchen)
            RefundKitchenJob(ent, job);
        else
            RefundBuffer(ent, job);

        StopJob(ent);
    }

    private void OnFlushBufferMessage(Entity<AutoCookerComponent> ent, ref AutoCookerFlushBufferMessage args)
    {
        FlushBuffer(ent);
    }

    private void OnFillBufferMessage(Entity<AutoCookerComponent> ent, ref AutoCookerFillBufferMessage args)
    {
        FillBuffer(ent);
    }

    private void OnRemoveQueuedMessage(Entity<AutoCookerComponent> ent, ref AutoCookerRemoveQueuedMessage args)
    {
        if (args.Index < 0 || args.Index >= ent.Comp.Queue.Count)
            return;

        ent.Comp.Queue.RemoveAt(args.Index);
        UpdateVisuals(ent);
        UpdateUi(ent);
    }

    /// <summary>
    /// Сигнал запуска от автоматизации повторяет последний заказ.
    /// </summary>
    private void OnMachineStarted(Entity<AutoCookerComponent> ent, ref MachineStartedEvent args)
    {
        if (ent.Comp.LastOrder is not { } order || !TryOrder(ent, order))
            _startable.Failed(ent.Owner);
    }

    /// <summary>
    /// Запускает заказ сразу или ставит его в очередь, если машина занята.
    /// </summary>
    private bool TryOrder(Entity<AutoCookerComponent> ent, AutoCookOrder order)
    {
        if (!_power.IsPowered(ent.Owner))
            return false;

        if (ent.Comp.Job == null && ent.Comp.Queue.Count == 0)
        {
            if (TryBegin(ent, order))
                return true;

            UpdateUi(ent);
            return false;
        }

        if (ent.Comp.Queue.Count >= ent.Comp.MaxQueue)
            return false;

        ent.Comp.Queue.Add(order);
        UpdateVisuals(ent);
        UpdateUi(ent);
        return true;
    }

    private void TryStartQueued(Entity<AutoCookerComponent> ent)
    {
        if (!_power.IsPowered(ent.Owner))
            return;

        // Синтезу некуда сливать результат, ждём, пока вставят пустой стакан
        if (ent.Comp.Kind != AutoCookKind.Kitchen && GetBeakerSpace(ent) < 1)
            return;

        var order = ent.Comp.Queue[0];
        ent.Comp.Queue.RemoveAt(0);

        if (TryBegin(ent, order))
            return;

        _popup.PopupEntity(Loc.GetString("autocook-queue-failed", ("name", GetResultName(order.Recipe))), ent);
        _startable.Failed(ent.Owner);
        UpdateVisuals(ent);
        UpdateUi(ent);
    }

    private bool TryBegin(Entity<AutoCookerComponent> ent, AutoCookOrder order)
    {
        EnsureIndex();

        var job = ent.Comp.Kind == AutoCookKind.Kitchen
            ? TryCreateKitchenJob(ent, order.Recipe)
            : TryCreateReagentJob(ent, order);

        if (job == null)
            return false;

        if (TryComp<ApcPowerReceiverComponent>(ent, out var receiver))
        {
            job.IdleLoad = receiver.Load;
            _power.SetLoad(ent.Owner, ent.Comp.WorkingLoad);
        }

        job.StepStart = _timing.CurTime;
        ent.Comp.Job = job;

        _audio.PlayPvs(ent.Comp.StartSound, ent);
        _startable.Started(ent.Owner);
        UpdateVisuals(ent);
        UpdateUi(ent);
        return true;
    }

    private void Pause(Entity<AutoCookerComponent> ent, AutoCookJob job, TimeSpan now)
    {
        if (job.PausedElapsed != null)
            return;

        job.PausedElapsed = now - job.StepStart;
        UpdateUi(ent);
    }

    private void Resume(Entity<AutoCookerComponent> ent, AutoCookJob job, TimeSpan now)
    {
        if (job.PausedElapsed is not { } elapsed)
            return;

        job.PausedElapsed = null;
        job.StepStart = now - elapsed;
        UpdateUi(ent);
    }

    private void AdvanceStep(Entity<AutoCookerComponent> ent, AutoCookJob job, TimeSpan now)
    {
        ApplyStep(job, job.Steps[job.StepIndex]);
        job.StepIndex++;
        job.StepStart = now;

        _audio.PlayPvs(ent.Comp.StepSound, ent);
        UpdateUi(ent);
    }

    private void TryFinish(Entity<AutoCookerComponent> ent, AutoCookJob job)
    {
        if (ent.Comp.Kind == AutoCookKind.Kitchen)
        {
            var coords = Transform(ent).Coordinates;
            for (var i = 0; i < job.ResultCount; i++)
            {
                Spawn(job.Result, coords);
            }

            ClearProcessing(ent);
        }
        else if (!TryOutputReagent(ent, job))
        {
            if (!job.WaitingOutput)
            {
                job.WaitingOutput = true;
                UpdateUi(ent);
            }

            return;
        }

        _audio.PlayPvs(ent.Comp.DoneSound, ent);
        ent.Comp.DoneUntil = _timing.CurTime + ent.Comp.DoneDuration;
        StopJob(ent);
        _startable.Completed(ent.Owner, autoStart: ent.Comp.Queue.Count == 0);
    }

    private void StopJob(Entity<AutoCookerComponent> ent)
    {
        if (ent.Comp.Job is { } job)
            _power.SetLoad(ent.Owner, job.IdleLoad);

        ent.Comp.Job = null;
        UpdateVisuals(ent);
        UpdateUi(ent);
    }

    private void UpdateVisuals(Entity<AutoCookerComponent> ent)
    {
        AutoCookerVisualState state;
        if (!_power.IsPowered(ent.Owner))
            state = AutoCookerVisualState.Off;
        else if (ent.Comp.DoneUntil != null)
            state = AutoCookerVisualState.Done;
        else if (ent.Comp.Job != null || ent.Comp.Queue.Count > 0)
            state = AutoCookerVisualState.Cooking;
        else
            state = AutoCookerVisualState.Normal;

        _appearance.SetData(ent.Owner, AutoCookerVisuals.State, state);
    }
}
