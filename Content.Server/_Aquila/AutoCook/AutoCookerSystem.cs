using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Power.Components;
using Content.Shared._Aquila.AutoCook;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
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
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoCookerComponent, MapInitEvent>(OnMapInit, before: [typeof(ItemSlotsSystem)]);
        SubscribeLocalEvent<AutoCookerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AutoCookerComponent, BoundUIOpenedEvent>(OnUiRefresh);
        SubscribeLocalEvent<AutoCookerComponent, EntInsertedIntoContainerMessage>(OnUiRefresh);
        SubscribeLocalEvent<AutoCookerComponent, EntRemovedFromContainerMessage>(OnUiRefresh);
        SubscribeLocalEvent<AutoCookerComponent, SolutionContainerChangedEvent>(OnUiRefresh);
        SubscribeLocalEvent<AutoCookerComponent, PowerChangedEvent>(OnUiRefresh);
        SubscribeLocalEvent<AutoCookerComponent, AutoCookerStartMessage>(OnStart);
        SubscribeLocalEvent<AutoCookerComponent, AutoCookerCancelMessage>(OnCancel);
        SubscribeLocalEvent<AutoCookerComponent, AutoCookerRemoveQueuedMessage>(OnRemoveQueued);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnMapInit(Entity<AutoCookerComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Kind != AutoCookKind.Kitchen)
            _itemSlots.AddItemSlot(ent.Owner, AutoCookerComponent.BeakerSlotName, ent.Comp.BeakerSlot);
    }

    private void OnShutdown(Entity<AutoCookerComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Kind != AutoCookKind.Kitchen)
            _itemSlots.RemoveItemSlot(ent.Owner, ent.Comp.BeakerSlot);
        else if (ent.Comp.Job is { } job && job.StepIndex < job.Steps.Count)
            DropKitchenJob(ent, job);
    }

    private void OnUiRefresh<T>(Entity<AutoCookerComponent> ent, ref T args)
    {
        UpdateUi(ent);
    }

    private void OnStart(Entity<AutoCookerComponent> ent, ref AutoCookerStartMessage args)
    {
        if (!_power.IsPowered(ent.Owner))
            return;

        EnsureIndex();

        if (ent.Comp.Job != null || ent.Comp.Queue.Count > 0)
        {
            if (ent.Comp.Queue.Count < ent.Comp.MaxQueue)
                ent.Comp.Queue.Add(new AutoCookOrder(args.RecipeId, args.Amount));

            UpdateUi(ent);
            return;
        }

        if (!TryBegin(ent, args.RecipeId, args.Amount))
            UpdateUi(ent);
    }

    private bool TryBegin(Entity<AutoCookerComponent> ent, string recipeId, int amount)
    {
        var job = ent.Comp.Kind == AutoCookKind.Kitchen
            ? TryStartKitchen(ent, recipeId)
            : TryStartReagent(ent, recipeId, amount);

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
        UpdateUi(ent);
        return true;
    }

    private void OnRemoveQueued(Entity<AutoCookerComponent> ent, ref AutoCookerRemoveQueuedMessage args)
    {
        if (args.Index < 0 || args.Index >= ent.Comp.Queue.Count)
            return;

        ent.Comp.Queue.RemoveAt(args.Index);
        UpdateUi(ent);
    }

    private void TryStartQueued(Entity<AutoCookerComponent> ent)
    {
        if (ent.Comp.Kind != AutoCookKind.Kitchen && GetBeakerSpace(ent) < FixedPoint2.New(1))
            return;

        EnsureIndex();

        var order = ent.Comp.Queue[0];
        ent.Comp.Queue.RemoveAt(0);

        if (TryBegin(ent, order.RecipeId, order.Amount))
            return;

        _popup.PopupEntity(Loc.GetString("autocook-queue-failed", ("name", GetRecipeName(order.RecipeId))), ent);
        UpdateUi(ent);
    }

    private void OnCancel(Entity<AutoCookerComponent> ent, ref AutoCookerCancelMessage args)
    {
        if (ent.Comp.Job is not { } job)
            return;

        RefundKitchenJob(ent, job);
        StopJob(ent);
    }

    private AutoCookJob? TryStartReagent(Entity<AutoCookerComponent> ent, string recipeId, int amount)
    {
        if (!recipeId.StartsWith(AutoCookIds.Reagent))
            return null;

        var reagent = recipeId[AutoCookIds.Reagent.Length..];
        if (!_proto.TryIndex<ReagentPrototype>(reagent, out var reagentProto)
            || !IsCraftableTarget(ent.Comp, reagentProto))
            return null;

        var requested = MathF.Min(amount, GetBeakerSpace(ent).Float());
        if (requested < 1f)
            return null;

        var raw = new List<AutoCookStep>();
        if (!TryPlanReagent(ent.Comp, reagent, requested, raw) || PlanUsesRestricted(ent.Comp, raw))
            return null;

        var steps = FinalizeReagentSteps(ent.Comp, raw);
        if (steps.Count == 0)
            return null;

        return new AutoCookJob
        {
            RecipeName = ReagentName(reagent),
            Steps = steps,
            TargetReagent = reagent,
            TargetAmount = requested,
        };
    }

    private FixedPoint2 GetBeakerSpace(Entity<AutoCookerComponent> ent)
    {
        var beaker = _itemSlots.GetItemOrNull(ent, AutoCookerComponent.BeakerSlotName);
        return beaker != null && _solution.TryGetFitsInDispenser(beaker.Value, out _, out var solution)
            ? solution.AvailableVolume
            : FixedPoint2.Zero;
    }

    private void StopJob(Entity<AutoCookerComponent> ent)
    {
        if (ent.Comp.Job is { } job)
            _power.SetLoad(ent.Owner, job.IdleLoad);

        ent.Comp.Job = null;
        UpdateUi(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<AutoCookerComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            var ent = (uid, comp);

            RefreshVisuals(ent, now);

            if (comp.Job is not { } job)
            {
                if (comp.Queue.Count > 0 && _power.IsPowered(uid))
                    TryStartQueued(ent);

                continue;
            }

            if (!_power.IsPowered(uid))
            {
                if (!job.Paused)
                {
                    job.Paused = true;
                    job.PausedElapsed = now - job.StepStart;
                    UpdateUi(ent);
                }

                continue;
            }

            if (job.Paused)
            {
                job.Paused = false;
                job.StepStart = now - job.PausedElapsed;
                UpdateUi(ent);
            }

            if (job.StepIndex >= job.Steps.Count)
            {
                TryFinish(ent, job);
                continue;
            }

            var step = job.Steps[job.StepIndex];
            if ((now - job.StepStart).TotalSeconds < step.Duration)
                continue;

            ApplyStep(job, step);
            job.StepIndex++;
            job.StepStart = now;
            _audio.PlayPvs(comp.StepSound, uid);
            UpdateUi(ent);

            if (job.StepIndex >= job.Steps.Count)
                TryFinish(ent, job);
        }
    }

    private void RefreshVisuals(Entity<AutoCookerComponent> ent, TimeSpan now)
    {
        var state = !_power.IsPowered(ent.Owner)
            ? AutoCookerVisualState.Off
            : ent.Comp.DoneUntil > now
                ? AutoCookerVisualState.Done
                : ent.Comp.Job != null || ent.Comp.Queue.Count > 0
                    ? AutoCookerVisualState.Cooking
                    : AutoCookerVisualState.Normal;

        if (ent.Comp.Visual == state)
            return;

        ent.Comp.Visual = state;
        _appearance.SetData(ent.Owner, AutoCookerVisuals.State, state);
    }

    private static void ApplyStep(AutoCookJob job, AutoCookStep step)
    {
        foreach (var (id, amount) in step.Consume)
        {
            job.Stock[id] = MathF.Max(0f, job.Stock.GetValueOrDefault(id) - amount);
        }

        foreach (var (id, amount) in step.Produce)
        {
            job.Stock[id] = job.Stock.GetValueOrDefault(id) + amount;
        }
    }

    private void TryFinish(Entity<AutoCookerComponent> ent, AutoCookJob job)
    {
        if (ent.Comp.Kind == AutoCookKind.Kitchen)
        {
            for (var i = 0; i < job.ResultCount && job.ResultEntity != null; i++)
            {
                Spawn(job.ResultEntity, Transform(ent).Coordinates);
            }

            _audio.PlayPvs(ent.Comp.DoneSound, ent);
            ent.Comp.DoneUntil = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.DoneSeconds);
            StopJob(ent);
            return;
        }

        var beaker = _itemSlots.GetItemOrNull(ent, AutoCookerComponent.BeakerSlotName);
        if (beaker == null
            || job.TargetReagent == null
            || !_solution.TryGetFitsInDispenser(beaker.Value, out var soln, out var solution)
            || solution.AvailableVolume <= FixedPoint2.Zero)
        {
            if (!job.WaitingOutput)
            {
                job.WaitingOutput = true;
                UpdateUi(ent);
            }

            return;
        }

        var produced = MathF.Min(job.TargetAmount, job.Stock.GetValueOrDefault(job.TargetReagent));
        var amount = FixedPoint2.Min(FixedPoint2.New(produced), solution.AvailableVolume);
        if (amount > FixedPoint2.Zero)
            _solution.TryAddReagent(soln.Value, job.TargetReagent, amount);

        _audio.PlayPvs(ent.Comp.DoneSound, ent);
        ent.Comp.DoneUntil = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.DoneSeconds);
        StopJob(ent);
    }
}
