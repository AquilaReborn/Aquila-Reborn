using System;
using Content.Server._Aquila.WOU.Stand;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Guardian;
using Content.Shared.Actions;
using Content.Shared._Aquila.WOU.Switching;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.Flash;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Pain.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Consciousness.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.WOU.Switching;

public sealed class SwitchingParasiteSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly WoundSystem _wounds = default!;

    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly PainSystem _pain = default!;
    [Dependency] private readonly ConsciousnessSystem _consciousness = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SwitchingParasiteComponent, ComponentStartup>(OnParasiteStartup);
        SubscribeLocalEvent<SwitchingParasiteComponent, ComponentShutdown>(OnParasiteShutdown);
        SubscribeLocalEvent<SwitchingParasiteComponent, SwitchActionEvent>(OnSwitchAction);
        SubscribeLocalEvent<SwitchingComponent, SwitchReturnActionEvent>(OnReturnAction);
        SubscribeLocalEvent<SwitchingComponent, MobStateChangedEvent>(OnHumanStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<SwitchingParasiteComponent, GuardianComponent>();
        while (query.MoveNext(out var parasite, out var comp, out var guardian))
        {
            if (comp.SwitchInitialized) continue;
            if (guardian.Host is not { } host || !Exists(host)) continue;
            if (!HasComp<GuardianHostComponent>(host)) continue;

            _actions.AddAction(parasite, ref comp.SwitchActionEntity, comp.SwitchActionProto);
            comp.SwitchInitialized = true;
        }
    }

    private void OnParasiteStartup(EntityUid uid, SwitchingParasiteComponent comp, ComponentStartup args)
    {
        RemComp<GhostRoleComponent>(uid);
        RemComp<GhostTakeoverAvailableComponent>(uid);
    }

    private void OnParasiteShutdown(EntityUid uid, SwitchingParasiteComponent comp, ComponentShutdown args)
    {
        if (comp.SwitchActionEntity is { } action) _actions.RemoveAction((action, null));
        if (!TryComp<SwitchingComponent>(uid, out var sw)) return;

        var partner = sw.Partner;
        if (Exists(partner) && TryComp<SwitchingComponent>(partner, out var partnerSw))
        {
            if (partnerSw.ReturnActionEntity is { } theirAction) _actions.RemoveAction((theirAction, null));
            RemComp<SwitchingComponent>(partner);
        }
    }

    private void OnSwitchAction(EntityUid uid, SwitchingParasiteComponent comp, SwitchActionEvent args)
    {
        if (args.Handled) return;

        if (!TryComp<GuardianComponent>(uid, out var guardian) || guardian.Host is not { } host)
        {
            _popup.PopupEntity(Loc.GetString("switching-parasite-no-host"), uid, uid);
            return;
        }
        if (!TryComp<GuardianHostComponent>(host, out var hostComp))
        {
            _popup.PopupEntity(Loc.GetString("switching-parasite-no-host"), uid, uid);
            return;
        }

        comp.NextAutoReturnAllowed = _timing.CurTime + comp.AutoReturnCooldown;

        if (TryComp<WOUParasiteModeComponent>(uid, out var switchModeComp))
        {
            _audio.PlayPvs(switchModeComp.SwitchSound, uid);
            _flash.FlashArea(uid, null, switchModeComp.FlashRange, switchModeComp.FlashDuration);
        }

        if (hostComp.GuardianContainer.Contains(uid))
        {
            _containers.Remove(uid, hostComp.GuardianContainer);
            guardian.GuardianLoose = true;
        }

        var humanCoords = _transform.GetMapCoordinates(uid);
        EntityUid human;

        if (comp.CurrentHuman is { } existing && Exists(existing))
        {
            human = existing;
            if (_containers.TryGetContainer(host, comp.HumanContainerId, out var hostContainer))
            {
                _containers.Remove(human, hostContainer);
            }
            _transform.SetMapCoordinates(human, humanCoords);
        }
        else
        {
            human = Spawn(comp.HumanProto, humanCoords);
            comp.CurrentHuman = human;
        }

        if (_mind.TryGetMind(uid, out var mindId, out var mindComp))
            _mind.TransferTo(mindId, human, mind: mindComp);

        var parasiteContainer = _containers.EnsureContainer<ContainerSlot>(human, comp.ParasiteContainerId);
        _containers.Insert(uid, parasiteContainer);
        guardian.GuardianLoose = true;

        var humanSwitch = EnsureComp<SwitchingComponent>(human);
        humanSwitch.Partner = uid;
        humanSwitch.ReturnActionProto = comp.ReturnActionProto;

        var parasiteSwitch = EnsureComp<SwitchingComponent>(uid);
        parasiteSwitch.Partner = human;
        parasiteSwitch.ReturnActionProto = comp.ReturnActionProto;

        _actions.AddAction(human, ref humanSwitch.ReturnActionEntity, comp.ReturnActionProto);
        if (humanSwitch.ReturnActionEntity is { } returnAction) _actions.StartUseDelay(returnAction);

        _popup.PopupEntity(Loc.GetString("switching-parasite-switch-success"), human, human);

        var healDelay = comp.HealDelay;
        Timer.Spawn(healDelay, () =>
        {
            HealEntityCompletely(human);
            HealEntityCompletely(uid);
        });

        args.Handled = true;
    }

    private void OnReturnAction(EntityUid uid, SwitchingComponent comp, SwitchReturnActionEvent args)
    {
        if (args.Handled) return;
        args.Handled = true;

        var partner = comp.Partner;
        if (!Exists(partner) || !TryComp<SwitchingComponent>(partner, out var partnerComp))
        {
            RemComp<SwitchingComponent>(uid);
            return;
        }
        if (!TryComp<SwitchingParasiteComponent>(partner, out var parasiteComp))
        {
            RemComp<SwitchingComponent>(uid);
            return;
        }
        DoReturn(uid, partner, comp, partnerComp, parasiteComp);
    }

    private void OnHumanStateChanged(EntityUid uid, SwitchingComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Critical || args.NewMobState == MobState.Dead)
        {
            var partner = comp.Partner;
            if (!Exists(partner) || !TryComp<SwitchingComponent>(partner, out var partnerComp)) return;
            if (!TryComp<SwitchingParasiteComponent>(partner, out var parasiteComp)) return;

            if (_timing.CurTime < parasiteComp.NextAutoReturnAllowed)
                return;

            DoReturn(uid, partner, comp, partnerComp, parasiteComp);
        }
    }

    private void DoReturn(EntityUid human, EntityUid parasite, SwitchingComponent humanComp, SwitchingComponent parasiteComp, SwitchingParasiteComponent parasiteCompData)
    {
        if (humanComp.ReturnActionEntity is { } myAction) _actions.RemoveAction((myAction, null));
        if (parasiteComp.ReturnActionEntity is { } theirAction) _actions.RemoveAction((theirAction, null));

        if (TryComp<WOUParasiteModeComponent>(parasite, out var switchModeComp))
        {
            _audio.PlayPvs(switchModeComp.SwitchSound, parasite);
            _flash.FlashArea(parasite, null, switchModeComp.FlashRange, switchModeComp.FlashDuration);
        }

        if (_containers.TryGetContainer(human, parasiteCompData.ParasiteContainerId, out var parasiteContainer))
            _containers.Remove(parasite, parasiteContainer);

        var humanCoords = _transform.GetMapCoordinates(human);
        _transform.SetMapCoordinates(parasite, humanCoords);

        if (_mind.TryGetMind(human, out var mindId, out var mindComp))
            _mind.TransferTo(mindId, parasite, mind: mindComp);

        if (TryComp<GuardianComponent>(parasite, out var guardian) && guardian.Host is { } host)
        {
            var humanContainer = _containers.EnsureContainer<ContainerSlot>(host, parasiteCompData.HumanContainerId);
            _containers.Insert(human, humanContainer);
        }

        _transform.SetCoordinates(human, EntityCoordinates.Invalid);
        parasiteCompData.CurrentHuman = human;

        RemComp<SwitchingComponent>(parasite);
        RemComp<SwitchingComponent>(human);

        if (parasiteCompData.SwitchActionEntity is { } switchAction) _actions.StartUseDelay(switchAction);

        _popup.PopupEntity(Loc.GetString("switching-parasite-return-success"), parasite, parasite);

        var healDelay = parasiteCompData.HealDelay;
        Timer.Spawn(healDelay, () =>
        {
            HealEntityCompletely(parasite);
            HealEntityCompletely(human);
        });
    }

    private void HealEntityCompletely(EntityUid uid)
    {
        if (!Exists(uid)) return;

        _damageable.ClearAllDamage(uid);
        _wounds.TryHaltAllBleeding(uid, force: true);

        if (_consciousness.TryGetNerveSystem(uid, out var nerveSys))
        {
            _pain.RemoveAllPainEffects(nerveSys.Value);
        }

        if (TryComp<BloodstreamComponent>(uid, out var bloodstream) &&
            _solutionContainer.ResolveSolution(uid, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution))
        {
            var missingVolume = bloodSolution.AvailableVolume;
            if (missingVolume > 0)
            {
                _bloodstream.TryModifyBloodLevel((uid, bloodstream), missingVolume);
            }
        }

        var partQuery = EntityQueryEnumerator<BodyPartComponent>();
        while (partQuery.MoveNext(out var partUid, out var partComp))
        {
            if (partComp.Body == uid && Exists(partUid))
            {
                _damageable.ClearAllDamage(partUid);
                _wounds.TryHaltAllBleeding(partUid, force: true);
            }
        }
    }
}
