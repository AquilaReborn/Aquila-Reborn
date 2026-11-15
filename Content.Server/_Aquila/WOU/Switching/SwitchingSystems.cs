using System;
using Content.Server.Aquila.WOU.Stand;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Guardian;
using Content.Server.Humanoid;
using Content.Shared.Actions;
using Content.Shared.Aquila.WOU.Switching;
using Content.Shared.DetailExaminable;
using Content.Shared.Flash;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Aquila.WOU.Switching;

/// <summary>
/// Система переключения между паразитом и телом Сатору.
/// </summary>
public sealed class SwitchingParasiteSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly WOUParasiteModeSystem _wouMode = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

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
            if (comp.Initialized) continue;
            if (guardian.Host is not { } host || !Exists(host)) continue;
            if (!HasComp<GuardianHostComponent>(host)) continue;

            _actions.AddAction(parasite, ref comp.SwitchActionEntity, comp.SwitchActionProto);
            comp.Initialized = true;
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

        // Устанавливаем 2-секундный иммунитет к автоматическому возврату сразу после Switch
        comp.NextAutoReturnAllowed = _timing.CurTime + TimeSpan.FromSeconds(2);

        if (TryComp<WOUParasiteModeComponent>(uid, out var modeComp) && !modeComp.IsModeTwo)
            _wouMode.ApplyMode(uid, modeComp, true, playEffects: false);

        if (TryComp<WOUParasiteModeComponent>(uid, out var switchModeComp))
        {
            _audio.PlayPvs(switchModeComp.SwitchSound, uid);
            _flash.FlashArea(uid, null, switchModeComp.FlashRange, TimeSpan.FromSeconds(1));
        }

        if (hostComp.GuardianContainer.Contains(uid))
        {
            _containers.Remove(uid, hostComp.GuardianContainer);
            guardian.GuardianLoose = true;
        }

        EntityUid human;
        if (comp.CurrentHuman is { } existing && Exists(existing))
        {
            human = existing;
            _transform.SetMapCoordinates(human, _transform.GetMapCoordinates(uid));
        }
        else
        {
            human = Spawn(comp.HumanProto, _transform.GetMapCoordinates(uid));
            comp.CurrentHuman = human;
        }

        // УДАЛЕНО: Блоки выдачи Godmode, полного лечения и снятия Godmode с задержками.

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

            // Проверяем 2-секундный иммунитет, установленный в момент Switch
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
            _flash.FlashArea(parasite, null, switchModeComp.FlashRange, TimeSpan.FromSeconds(1));
        }

        // УДАЛЕНО: Блок снижения урона (лечения на 10 единиц) при возврате.

        if (_containers.TryGetContainer(human, parasiteCompData.ParasiteContainerId, out var parasiteContainer))
            _containers.Remove(parasite, parasiteContainer);

        var humanCoords = _transform.GetMapCoordinates(human);
        _transform.SetMapCoordinates(parasite, humanCoords); 

        if (_mind.TryGetMind(human, out var mindId, out var mindComp))
            _mind.TransferTo(mindId, parasite, mind: mindComp);

        _transform.SetCoordinates(human, EntityCoordinates.Invalid);
        parasiteCompData.CurrentHuman = human;

        RemComp<SwitchingComponent>(parasite);
        RemComp<SwitchingComponent>(human);

        if (parasiteCompData.SwitchActionEntity is { } switchAction) _actions.StartUseDelay(switchAction);

        _popup.PopupEntity(Loc.GetString("switching-parasite-return-success"), parasite, parasite);
    }
}

/// <summary>
/// Система применения профиля внешности и выдачи одежды Сатору.
/// </summary>
public sealed class SwitchingHumanProfileSystem : EntitySystem
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SwitchingHumanProfileComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, SwitchingHumanProfileComponent comp, MapInitEvent args)
    {
        _humanoid.LoadProfile(uid, comp.Profile);

        if (!string.IsNullOrEmpty(comp.Profile.Name))
            _meta.SetEntityName(uid, comp.Profile.Name);

        if (!string.IsNullOrEmpty(comp.Profile.FlavorText))
        {
            var detail = EnsureComp<DetailExaminableComponent>(uid);
            detail.Content = comp.Profile.FlavorText;
        }

        var coords = _transform.GetMapCoordinates(uid);
        var gear = new Dictionary<string, string>
        {
            { "head", "ClothingHeadHatAquilaBowler" },
            { "jumpsuit", "ClothingUniformJumpsuitAquilaBusiness" },
            { "neck", "ClothingNeckScarfAquilaRed" },
            { "gloves", "ClothingHandsGlovesColorWhite" },
            { "shoes", "ClothingShoesColorWhite" },
            { "id", "PassengerIDCardSatoru" }
        };

        foreach (var (slot, proto) in gear)
        {
            var item = Spawn(proto, coords);
            _inventory.TryEquip(uid, item, slot, silent: true);
        }
    }
}