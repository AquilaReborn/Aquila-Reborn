using System.Numerics;
using Content.Server.NPC;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared._Goobstation.Wizard.Guardian;
using Content.Shared.Aquila.Guardian;
using Content.Server.Aquila.Guardian;
using Content.Shared.CombatMode;
using Content.Shared.Pointing;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Guardian;

public sealed partial class GuardianSystem
{
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;

    private const float MinRepositionDistanceSquared = 0.0001f;

    private void InitializeAquila()
    {
        SubscribeNetworkEvent<GuardianSetOrderEvent>(OnSetOrder);
        SubscribeLocalEvent<GuardianHostComponent, AfterPointedAtEvent>(OnHostPointedAt);
    }

    private void AddOrdersAction(EntityUid host, GuardianHostComponent component)
    {
        _actionSystem.AddAction(host, ref component.ActionOrdersEntity, component.ActionOrders);
    }

    private void RemoveOrdersAction(GuardianHostComponent component)
    {
        QueueDel(component.ActionOrdersEntity);
        component.ActionOrdersEntity = null;
    }

    private void StartGuardianOrders(EntityUid guardian, EntityUid host, GuardianComponent component)
    {
        _npc.SetBlackboard(guardian, NPCBlackboard.FollowTarget, new EntityCoordinates(host, Vector2.Zero));
        UpdateGuardianNpc(guardian, component);
    }

    private void RepositionGuardian(EntityUid guardianUid, GuardianComponent guardian, TransformComponent guardianXform, EntityCoordinates coordinates)
    {
        guardian.IsBeginRepositioned = true;
        try
        {
            _transform.SetCoordinates(guardianUid, guardianXform, coordinates);
        }
        finally
        {
            guardian.IsBeginRepositioned = false;
        }
    }

    private void PullGuardianToHost(EntityUid guardianUid, GuardianComponent guardian, TransformComponent guardianXform, Vector2 hostPos, Vector2 diff)
    {
        if (diff.LengthSquared() < MinRepositionDistanceSquared)
            return;

        var newDiff = diff.Normalized() * guardian.DistanceAllowed;
        guardian.IsBeginRepositioned = true;
        try
        {
            _transform.SetLocalPosition(guardianUid, hostPos + newDiff, guardianXform);
        }
        finally
        {
            guardian.IsBeginRepositioned = false;
        }
    }

    /// <summary>
    /// Создаёт стража указанного прототипа внутри цели и делает её хозяином. Возвращает стража или null, если не получилось.
    /// </summary>
    public EntityUid? CreateGuardian(EntityUid target, string guardianProto)
    {
        if (HasComp<GuardianHostComponent>(target))
            return null;

        var hostXform = Transform(target);
        var host = EnsureComp<GuardianHostComponent>(target);
        var guardian = Spawn(guardianProto, _transform.GetMapCoordinates(target, xform: hostXform));

        _faction.IgnoreEntity(guardian, target);
        var sharedComp = EnsureComp<GuardianSharedComponent>(guardian);
        sharedComp.Host = target;
        Dirty(guardian, sharedComp);

        _container.Insert(guardian, host.GuardianContainer);
        host.HostedGuardian = guardian;

        if (TryComp<GuardianComponent>(guardian, out var guardianComp))
        {
            guardianComp.Host = target;
            StartGuardianOrders(guardian, target, guardianComp);
            _audio.PlayPvs(guardianComp.InjectSound, target);
            _popupSystem.PopupEntity(Loc.GetString("guardian-created"), target, target);
            return guardian;
        }

        QueueDel(guardian);
        QueueDel(host.ActionEntity);
        RemComp<GuardianHostComponent>(target);
        return null;
    }

    private void OnSetOrder(GuardianSetOrderEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        if (!TryComp<GuardianHostComponent>(user, out var hostComponent) ||
            hostComponent.HostedGuardian is not { } guardian)
            return;

        if (!TryComp<GuardianComponent>(guardian, out var guardianComponent))
            return;

        if (guardianComponent.CurrentOrder == msg.Order)
            return;

        guardianComponent.CurrentOrder = msg.Order;
        UpdateGuardianNpc(guardian, guardianComponent);
    }

    private void OnHostPointedAt(EntityUid uid, GuardianHostComponent component, ref AfterPointedAtEvent args)
    {
        if (component.HostedGuardian is not { } guardian ||
            !TryComp<GuardianComponent>(guardian, out var guardianComponent) ||
            guardianComponent.CurrentOrder != GuardianOrderType.AttackTarget)
            return;

        _npc.SetBlackboard(guardian, NPCBlackboard.CurrentOrderedTarget, args.Pointed);
    }

    private void UpdateGuardianNpc(EntityUid guardian, GuardianComponent component)
    {
        if (TryComp<NPCRangedCombatComponent>(guardian, out _))
        {
            _combatMode.SetInCombatMode(guardian, false);
            RemComp<NPCRangedCombatComponent>(guardian);
        }

        _npc.SetBlackboard(guardian, NPCBlackboard.CurrentOrders, component.CurrentOrder);

        if (!TryComp<HTNComponent>(guardian, out var htn))
            return;

        if (htn.Plan != null)
            _htn.ShutdownPlan(htn);

        _htn.Replan(htn);
    }

    /// <summary>
    /// Возвращает стража обратно в носителя, если активен.
    /// </summary>
    public bool TryForceRetractGuardian(EntityUid host)
    {
        if (!TryComp<GuardianHostComponent>(host, out var hostComponent) ||
            hostComponent.HostedGuardian is not { } guardian ||
            !TryComp<GuardianComponent>(guardian, out var guardianComponent) ||
            !guardianComponent.GuardianLoose)
        {
            return false;
        }

        RetractGuardian(host, hostComponent, guardian, guardianComponent);
        return true;
    }

    private bool ShouldPersistGuardianOnDeath(EntityUid guardian)
    {
        return HasComp<GuardianPersistOnDeathComponent>(guardian);
    }
}
