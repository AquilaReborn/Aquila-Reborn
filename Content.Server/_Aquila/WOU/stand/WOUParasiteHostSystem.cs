using Content.Server.Guardian;
using Content.Shared.Actions;
using Content.Shared._Aquila.WOU.RemoteHoloParasite;
using Content.Shared._Aquila.WOU.Stand;
using Robust.Shared.GameObjects;

namespace Content.Server._Aquila.WOU.Stand;

public sealed class WOUParasiteHostSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WOUParasiteHostComponent, ComponentStartup>(OnWOUHostStartup);
        SubscribeLocalEvent<WOUParasiteHostComponent, ComponentShutdown>(OnWOUHostShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<WOUParasiteModeComponent, GuardianComponent>();
        while (query.MoveNext(out var parasite, out _, out var guardian))
        {
            if (guardian.Host is not { } host || !Exists(host)) continue;
            if (!HasComp<RemoteHoloParasiteHostComponent>(host)) continue;
            if (!HasComp<WOUParasiteHostComponent>(host)) EnsureComp<WOUParasiteHostComponent>(host);
        }
    }

    private void OnWOUHostStartup(EntityUid uid, WOUParasiteHostComponent comp, ComponentStartup args)
    {
        _actions.AddAction(uid, ref comp.BeeSphereActionEntity, comp.BeeSphereAction);
        _actions.AddAction(uid, ref comp.DoubleReflectActionEntity, comp.DoubleReflectAction);
        _actions.AddAction(uid, ref comp.CapsuleSphereActionEntity, comp.CapsuleSphereAction);
        _actions.AddAction(uid, ref comp.SlipSphereActionEntity, comp.SlipSphereAction);
    }

    private void OnWOUHostShutdown(EntityUid uid, WOUParasiteHostComponent comp, ComponentShutdown args)
    {
        if (comp.BeeSphereActionEntity is { } a2) _actions.RemoveAction(uid, a2);
        if (comp.DoubleReflectActionEntity is { } a3) _actions.RemoveAction(uid, a3);
        if (comp.CapsuleSphereActionEntity is { } a4) _actions.RemoveAction(uid, a4);
        if (comp.SlipSphereActionEntity is { } a5) _actions.RemoveAction(uid, a5);
    }
}