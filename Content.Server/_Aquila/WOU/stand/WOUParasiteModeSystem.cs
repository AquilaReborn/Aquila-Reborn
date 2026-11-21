using Content.Server.Aquila.LifeCreator;
using Content.Shared.Actions;
using Content.Shared._Aquila.WOU.Stand;
using Content.Shared.Flash;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;

namespace Content.Server._Aquila.WOU.Stand;

public sealed class WOUParasiteModeSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WOUParasiteModeComponent, ComponentStartup>(OnModeStartup);
        SubscribeLocalEvent<WOUParasiteModeComponent, ComponentShutdown>(OnModeShutdown);
    }

    private void OnModeStartup(EntityUid uid, WOUParasiteModeComponent comp, ComponentStartup args)
    {
        _actions.AddAction(uid, ref comp.BeeSphereActionEntity, comp.BeeSphereAction);
        _actions.AddAction(uid, ref comp.DoubleReflectActionEntity, comp.DoubleReflectAction);
        _actions.AddAction(uid, ref comp.CapsuleSphereActionEntity, comp.CapsuleSphereAction);
        _actions.AddAction(uid, ref comp.SlipSphereActionEntity, comp.SlipSphereAction);
        
        ApplyMode(uid, comp, playEffects: false);
    }

    private void OnModeShutdown(EntityUid uid, WOUParasiteModeComponent comp, ComponentShutdown args)
    {
        if (comp.BeeSphereActionEntity is { } beeAct) _actions.RemoveAction(uid, beeAct);
        if (comp.DoubleReflectActionEntity is { } drAct) _actions.RemoveAction(uid, drAct);
        if (comp.CapsuleSphereActionEntity is { } csAct) _actions.RemoveAction(uid, csAct);
        if (comp.SlipSphereActionEntity is { } ssAct) _actions.RemoveAction(uid, ssAct);
        
        if (!Exists(uid)) return;
        RemComp<ReflectComponent>(uid);
        RemComp<ReflectMeleeDamageComponent>(uid);
        RemComp<ItemToggleComponent>(uid);
    }

    public void ApplyMode(EntityUid uid, WOUParasiteModeComponent comp, bool playEffects = true)
    {
        _metaData.SetEntityName(uid, comp.ParasiteName);
        _speed.ChangeBaseSpeed(uid, comp.WalkSpeed, comp.SprintSpeed, 20f);

        var reflect = EnsureComp<ReflectComponent>(uid);
        reflect.ReflectProb = 1f;
        reflect.Spread = Angle.Zero;
        reflect.Reflects = ReflectType.Energy | ReflectType.NonEnergy | ReflectType.Magic;
        reflect.InRightPlace = true;
        Dirty(uid, reflect);
        
        var toggle = EnsureComp<ItemToggleComponent>(uid);
        toggle.Activated = true;
        Dirty(uid, toggle);
        
        EnsureComp<ReflectMeleeDamageComponent>(uid);

        if (playEffects)
        {
            _flash.FlashArea(uid, null, comp.FlashRange, comp.FlashDuration);
            _audio.PlayPvs(comp.SwitchSound, uid);
        }
    }
}