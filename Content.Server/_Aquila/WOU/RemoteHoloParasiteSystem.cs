using Content.Server.Guardian;
using Content.Shared.Actions;
using Content.Shared._Aquila.WOU.RemoteHoloParasite;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Containers;

namespace Content.Server._Aquila.WOU;

public sealed class RemoteHoloParasiteSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RemoteHoloParasiteHostComponent, ComponentStartup>(OnHostStartup);
        SubscribeLocalEvent<RemoteHoloParasiteHostComponent, ComponentShutdown>(OnHostShutdown);
        SubscribeLocalEvent<RemoteHoloParasiteHostComponent, SoulSwapActionEvent>(OnSwapAction);

        SubscribeLocalEvent<SoulSwappedComponent, SoulSwapReturnActionEvent>(OnReturnAction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RemoteHoloParasiteComponent, GuardianComponent>();
        while (query.MoveNext(out var parasite, out var marker, out var guardian))
        {
            if (marker.HostLinked)
                continue;

            if (guardian.Host is not { } host || !Exists(host))
                continue;

            if (!HasComp<GuardianHostComponent>(host))
                continue;

            marker.LinkedHost = host;
            marker.HostLinked = true;
            EnsureComp<RemoteHoloParasiteHostComponent>(host);
        }
    }

    private void OnHostStartup(EntityUid uid, RemoteHoloParasiteHostComponent comp, ComponentStartup args)
    {
        _actions.AddAction(uid, ref comp.SwapActionEntity, comp.SwapAction);
    }

    private void OnHostShutdown(EntityUid uid, RemoteHoloParasiteHostComponent comp, ComponentShutdown args)
    {
        if (comp.SwapActionEntity is { } action)
            _actions.RemoveAction((action, null));
        comp.SwapActionEntity = null;
    }

    private void OnSwapAction(EntityUid uid, RemoteHoloParasiteHostComponent hostComp, SoulSwapActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<GuardianHostComponent>(uid, out var guardianHost))
            return;

        if (guardianHost.HostedGuardian is not { } parasite)
            return;

        if (!TryComp<RemoteHoloParasiteComponent>(parasite, out _))
            return;

        if (!TryComp<GuardianComponent>(parasite, out var guardian))
            return;

        if (!guardian.GuardianLoose)
        {
            _container.Remove(parasite, guardianHost.GuardianContainer);
            guardian.GuardianLoose = true;
        }

        PerformSwap(uid, parasite);
        args.Handled = true;
    }

    private void OnReturnAction(EntityUid uid, SoulSwappedComponent comp, SoulSwapReturnActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var partner = comp.Partner;
        if (!Exists(partner) || !HasComp<SoulSwappedComponent>(partner))
            return;

        if (TryComp<SoulSwappedComponent>(partner, out var partnerComp))
        {
            if (partnerComp.ReturnActionEntity is { } theirAction)
                _actions.RemoveAction((theirAction, null));
            RemComp<SoulSwappedComponent>(partner);
        }

        if (comp.ReturnActionEntity is { } myAction)
            _actions.RemoveAction((myAction, null));
        RemComp<SoulSwappedComponent>(uid);

        SwapMinds(uid, partner);
    }

    private void PerformSwap(EntityUid host, EntityUid parasite)
    {
        if (!TryComp<RemoteHoloParasiteComponent>(parasite, out var parasiteComp))
            return;

        SwapMinds(host, parasite);

        var returnActionId = parasiteComp.ReturnAction;

        var hostSwap = EnsureComp<SoulSwappedComponent>(host);
        hostSwap.Partner = parasite;
        hostSwap.ReturnAction = returnActionId;

        var parasiteSwap = EnsureComp<SoulSwappedComponent>(parasite);
        parasiteSwap.Partner = host;
        parasiteSwap.ReturnAction = returnActionId;

        TryAddReturnAction(host, hostSwap);
        TryAddReturnAction(parasite, parasiteSwap);
    }

    private void TryAddReturnAction(EntityUid uid, SoulSwappedComponent swap)
    {
        if (!TryComp<MindContainerComponent>(uid, out var container) || !container.HasMind)
            return;

        _actions.AddAction(uid, ref swap.ReturnActionEntity, swap.ReturnAction);
    }

    private void SwapMinds(EntityUid a, EntityUid b)
    {
        var hasMindA = _mind.TryGetMind(a, out var mindAId, out var mindAComp);
        var hasMindB = _mind.TryGetMind(b, out var mindBId, out var mindBComp);

        if (hasMindB)
            _mind.TransferTo(mindBId, a, mind: mindBComp);

        if (hasMindA)
            _mind.TransferTo(mindAId, b, mind: mindAComp);
    }
}