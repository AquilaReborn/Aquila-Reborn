using System.Linq;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared._Aquila.Kitchen;
using Content.Shared._Aquila.Kitchen.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Kitchen;

/// <summary>
/// Мясорубка. Труп кладут на машину перетаскиванием или рукой, машина перемалывает его и выдаёт только
/// мясо: органы и прочие части из разделки выбрасываются.
/// </summary>
public sealed class MeatGrinderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> MeatTag = "Meat";
    private static readonly ProtoId<TagPrototype> OrganTag = "Organ";

    private static readonly LocId PopupNoPower = "meat-grinder-popup-no-power";
    private static readonly LocId PopupNotAnchored = "meat-grinder-popup-not-anchored";
    private static readonly LocId PopupBusy = "meat-grinder-popup-busy";
    private static readonly LocId PopupNotDead = "meat-grinder-popup-not-dead";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeatGrinderComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MeatGrinderComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<MeatGrinderComponent, DragDropTargetEvent>(OnDragDropTarget);
        SubscribeLocalEvent<MeatGrinderComponent, MeatGrinderLoadDoAfterEvent>(OnLoadDoAfter);
        SubscribeLocalEvent<MeatGrinderComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<MeatGrinderComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<MeatGrinderComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<MeatGrinderComponent>();

        while (query.MoveNext(out var uid, out var grinder))
        {
            if (grinder.GrindEnd is { } end && now >= end)
                FinishGrinding((uid, grinder));
        }
    }

    private void OnMapInit(Entity<MeatGrinderComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);

        _appearance.SetData(ent, MeatGrinderVisuals.IsWorking, false);
        _appearance.SetData(ent, MeatGrinderVisuals.IsBloody, ent.Comp.Bloody);
    }

    private void OnAfterInteractUsing(Entity<MeatGrinderComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (TryStartGrinding(ent, args.Used, args.User))
            args.Handled = true;
    }

    private void OnDragDropTarget(Entity<MeatGrinderComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        if (TryStartGrinding(ent, args.Dragged, args.User))
            args.Handled = true;
    }

    private void OnLoadDoAfter(Entity<MeatGrinderComponent> ent, ref MeatGrinderLoadDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ent.Comp.Loading = null;

        if (args.Cancelled || args.Used is not { } victim)
            return;

        if (!CanStartGrinding(ent, victim, args.User))
            return;

        DoStartGrinding(ent, victim, args.User);
    }

    private void OnPowerChanged(Entity<MeatGrinderComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered || ent.Comp.GrindEnd == null)
            return;

        CancelGrinding(ent);
    }

    private void OnUnanchorAttempt(Entity<MeatGrinderComponent> ent, ref UnanchorAttemptEvent args)
    {
        if (ent.Comp.GrindEnd != null)
            args.Cancel();
    }

    private void OnShutdown(Entity<MeatGrinderComponent> ent, ref ComponentShutdown args)
    {
        _audio.Stop(ent.Comp.GrindStream);
    }

    private bool TryStartGrinding(Entity<MeatGrinderComponent> ent, EntityUid victim, EntityUid user)
    {
        if (!CanStartGrinding(ent, victim, user))
            return false;

        ent.Comp.Loading = victim;

        var doAfterArgs = new DoAfterArgs(EntityManager, user, ent.Comp.LoadTime, new MeatGrinderLoadDoAfterEvent(), ent.Owner, target: ent.Owner, used: victim)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            RequireCanInteract = true,
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
            return true;

        ent.Comp.Loading = null;
        return false;
    }

    private bool CanStartGrinding(Entity<MeatGrinderComponent> ent, EntityUid victim, EntityUid user, bool quiet = false)
    {
        if (!HasComp<MobStateComponent>(victim))
            return false;

        if (ent.Comp.GrindEnd != null)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString(PopupBusy), ent, user);

            return false;
        }

        if (ent.Comp.Loading != null)
            return false;

        if (!_mobState.IsDead(victim))
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString(PopupNotDead), ent, user);

            return false;
        }

        if (!Transform(ent).Anchored)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString(PopupNotAnchored), ent, user);

            return false;
        }

        if (!TryComp<ApcPowerReceiverComponent>(ent, out var power) || !power.Powered)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString(PopupNoPower), ent, user);

            return false;
        }

        return true;
    }

    private void DoStartGrinding(Entity<MeatGrinderComponent> ent, EntityUid victim, EntityUid user)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container)
            || !_container.Insert(victim, container))
        {
            return;
        }

        ent.Comp.GrindEnd = _timing.CurTime + GetGrindTime(ent.Comp, victim);

        _appearance.SetData(ent, MeatGrinderVisuals.IsWorking, true);
        _jittering.AddJitter(ent, ent.Comp.JitterAmplitude, ent.Comp.JitterFrequency);
        _audio.PlayPvs(ent.Comp.StartSound, ent);
        ent.Comp.GrindStream = _audio.PlayPvs(ent.Comp.GrindSound, ent, AudioParams.Default.WithLoop(true))?.Entity;

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user):player} feeds {ToPrettyString(victim):target} to {ToPrettyString(ent):machine}");
    }

    private void CancelGrinding(Entity<MeatGrinderComponent> ent)
    {
        ent.Comp.GrindEnd = null;
        StopGrindingEffects(ent);

        if (_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            _container.EmptyContainer(container, destination: Transform(ent).Coordinates);
    }

    private void FinishGrinding(Entity<MeatGrinderComponent> ent)
    {
        ent.Comp.GrindEnd = null;
        StopGrindingEffects(ent);

        if (_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
        {
            foreach (var victim in container.ContainedEntities.ToArray())
            {
                GrindVictim(ent, victim);
                _container.Remove(victim, container, force: true);
                QueueDel(victim);
            }
        }

        if (!ent.Comp.Bloody)
            SetBloody(ent, true);

        _audio.PlayPvs(ent.Comp.FinishSound, ent);
    }

    private void GrindVictim(Entity<MeatGrinderComponent> ent, EntityUid victim)
    {
        SpillBlood(ent, victim);

        foreach (var item in _inventory.GetHandOrInventoryEntities(victim))
        {
            _transform.DropNextTo(item, ent.Owner);
        }

        SpawnMeat(ent, victim);
    }

    private void SpillBlood(Entity<MeatGrinderComponent> ent, EntityUid victim)
    {
        if (!TryComp<BloodstreamComponent>(victim, out var bloodstream))
            return;

        if (!_solution.TryGetSolution(victim, bloodstream.BloodSolutionName, out _, out var bloodSolution))
            return;

        _puddle.TrySplashSpillAt(ent, Transform(ent).Coordinates, bloodSolution, out _);
    }

    private void SpawnMeat(Entity<MeatGrinderComponent> ent, EntityUid victim)
    {
        var coords = Transform(ent).Coordinates;

        if (!TryComp<ButcherableComponent>(victim, out var butcherable))
        {
            SpawnFallbackMeat(ent.Comp, coords);
            return;
        }

        var meat = butcherable.SpawnedEntities.Where(IsMeat).ToList();
        if (meat.Count == 0)
        {
            SpawnFallbackMeat(ent.Comp, coords);
            return;
        }

        var spawned = EntitySpawnCollection.GetSpawns(meat, _random);
        var count = (int) MathF.Ceiling(spawned.Count * ent.Comp.MeatMultiplier);

        for (var i = 0; i < count; i++)
        {
            SpawnAtPosition(_random.Pick(spawned), coords);
        }
    }

    private void SpawnFallbackMeat(MeatGrinderComponent comp, EntityCoordinates coords)
    {
        for (var i = 0; i < comp.FallbackMeatAmount; i++)
        {
            SpawnAtPosition(comp.FallbackMeat, coords);
        }
    }

    private bool IsMeat(EntitySpawnEntry entry)
    {
        if (!_prototype.TryIndex(entry.PrototypeId, out var proto))
            return false;

        if (!proto.TryComp<TagComponent>(out var tags, _factory))
            return false;

        return _tag.HasTag(tags, MeatTag) && !_tag.HasTag(tags, OrganTag);
    }

    private TimeSpan GetGrindTime(MeatGrinderComponent comp, EntityUid victim)
    {
        if (!TryComp<PhysicsComponent>(victim, out var physics))
            return comp.GrindTime;

        return comp.GrindTime + TimeSpan.FromSeconds(comp.TimePerMass.TotalSeconds * physics.FixturesMass);
    }

    private void SetBloody(Entity<MeatGrinderComponent> ent, bool bloody)
    {
        ent.Comp.Bloody = bloody;
        Dirty(ent);
        _appearance.SetData(ent, MeatGrinderVisuals.IsBloody, bloody);
    }

    private void StopGrindingEffects(Entity<MeatGrinderComponent> ent)
    {
        _appearance.SetData(ent, MeatGrinderVisuals.IsWorking, false);
        RemCompDeferred<JitteringComponent>(ent);
        ent.Comp.GrindStream = _audio.Stop(ent.Comp.GrindStream);
    }
}
