using System.Linq;
using Content.Server._Aquila.Antags.RoleCleanup.Components;
using Content.Server._Aquila.Antags.RoleCleanup.DataDefinitions;
using Content.Server._Aquila.Antags.RoleCleanup.Events;
using Content.Server.Administration.Logs;
using Content.Server.Antag.Components;
using Content.Server.Traitor.Uplink;
using Content.Shared.Actions;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Store.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Antags.RoleCleanup.Systems;

/// <summary>
/// Общий механизм снятия антагонистских ролей: решает, что роль снята окончательно, и даёт правилам
/// убрать выданные компоненты, действия, фракции и записи в состоянии правила.
/// </summary>
public sealed class AntagRoleCleanupSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly NpcFactionSystem _factions = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedMindSystem _minds = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdded);
        SubscribeLocalEvent<RoleRemovedEvent>(OnRoleRemoved);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (Count<AntagRoleCleanupPendingComponent>() == 0)
            return;

        var pending = new List<EntityUid>();
        var query = EntityQueryEnumerator<AntagRoleCleanupPendingComponent>();
        while (query.MoveNext(out var mindId, out _))
            pending.Add(mindId);

        foreach (var mindId in pending)
        {
            if (TerminatingOrDeleted(mindId) || !TryComp<AntagRoleCleanupPendingComponent>(mindId, out var pendingComp))
                continue;

            RemComp<AntagRoleCleanupPendingComponent>(mindId);

            if (!TryComp<MindComponent>(mindId, out var mind))
                continue;

            var removed = pendingComp.RemovedRoles;
            var remaining = GetAntagRoles(mind);

            if (remaining.Count == 0)
                RemoveAllObjectives(mindId, mind, removed);
            else
                RemoveRoleObjectives(mindId, mind, removed);

            var body = GetMindBody(mindId);
            var ev = new AntagRoleCleanupEvent(mindId, mind, body, removed, remaining);
            RaiseLocalEvent(mindId, ev, true);
        }
    }

    /// <summary>
    /// Возвращает антагонистские прототипы всех ролей, которые сейчас висят на разуме.
    /// </summary>
    /// <param name="mind">Компонент разума.</param>
    /// <returns>Список прототипов антагонистов.</returns>
    private List<ProtoId<AntagPrototype>> GetAntagRoles(MindComponent mind)
    {
        var roles = new List<ProtoId<AntagPrototype>>();

        foreach (var roleEntity in mind.MindRoleContainer.ContainedEntities)
        {
            if (TryComp<MindRoleComponent>(roleEntity, out var role) && role.AntagPrototype is { } antag)
                roles.Add(antag);
        }

        return roles;
    }

    /// <summary>
    /// Возвращает тело, в котором сейчас находится разум, или null, если разум его покинул.
    /// </summary>
    /// <param name="mindId">Разум.</param>
    /// <returns>Тело разума или null.</returns>
    private EntityUid? GetMindBody(EntityUid mindId)
    {
        if (!TryComp<MindComponent>(mindId, out var mind))
            return null;

        if (mind.OwnedEntity is not { } body || TerminatingOrDeleted(body))
            return null;

        if (!TryComp<MindContainerComponent>(body, out var container) || container.Mind != mindId)
            return null;

        return body;
    }

    /// <summary>
    /// Убирает с сущности компоненты, которые выдала снятая роль.
    /// </summary>
    /// <param name="uid">Тело или разум.</param>
    /// <param name="types">Типы компонентов.</param>
    public void RemoveComponents(EntityUid uid, IReadOnlyList<Type> types)
    {
        foreach (var type in types)
            RemComp(uid, type);
    }

    /// <summary>
    /// Убирает с сущности действия с указанными прототипами.
    /// </summary>
    /// <param name="holder">Сущность, у которой убирают действия.</param>
    /// <param name="prototypes">Прототипы действий.</param>
    public void RemoveActions(EntityUid holder, IReadOnlyCollection<EntProtoId> prototypes)
    {
        var ids = prototypes.Select(prototype => prototype.Id).ToHashSet();

        foreach (var action in _actions.GetActions(holder).ToList())
        {
            if (MetaData(action.Owner).EntityPrototype?.ID is { } id && ids.Contains(id))
                _actions.RemoveAction(action.Owner);
        }
    }

    /// <summary>
    /// Убирает аплинк и магазин с надетых, удерживаемых и вживлённых предметов тела, включая вложенные контейнеры.
    /// </summary>
    /// <param name="body">Тело разума.</param>
    public void RemoveUplink(EntityUid body)
    {
        foreach (var item in EnumerateCarried(body))
            RemoveUplinkFrom(item);
    }

    private void RemoveUplinkFrom(EntityUid uid)
    {
        if (HasComp<UplinkComponent>(uid))
        {
            RemComp<UplinkComponent>(uid);
            RemComp<StoreComponent>(uid);
        }

        foreach (var container in _containers.GetAllContainers(uid))
        {
            foreach (var contained in container.ContainedEntities)
                RemoveUplinkFrom(contained);
        }
    }

    /// <summary>
    /// Запоминает фракции тела перед тем, как правило их поменяет.
    /// </summary>
    /// <param name="body">Тело разума.</param>
    /// <param name="role">Прототип выдаваемого антагониста.</param>
    public void RememberFactions(EntityUid body, ProtoId<AntagPrototype> role)
    {
        var snapshot = EnsureComp<AntagFactionSnapshotComponent>(body);

        foreach (var entry in snapshot.Snapshots)
        {
            if (entry.Role == role)
                return;
        }

        var record = new AntagFactionSnapshot
        {
            Role = role,
            HadComponent = TryComp<NpcFactionMemberComponent>(body, out var member)
        };

        if (record.HadComponent)
            record.Factions.AddRange(member!.Factions);

        snapshot.Snapshots.Add(record);
    }

    /// <summary>
    /// Возвращает телу фракции, которые были до выдачи ролей, если антагонистских ролей у него не осталось.
    /// </summary>
    /// <param name="body">Тело разума.</param>
    /// <param name="remaining">Антагонистские роли, которые остались у разума.</param>
    public void RestoreFactions(EntityUid body, IReadOnlyCollection<ProtoId<AntagPrototype>> remaining)
    {
        if (!TryComp<AntagFactionSnapshotComponent>(body, out var snapshot) || snapshot.Snapshots.Count == 0)
            return;

        foreach (var role in remaining)
        {
            foreach (var entry in snapshot.Snapshots)
            {
                if (entry.Role == role)
                    return;
            }
        }

        var record = snapshot.Snapshots[0];
        RemComp<AntagFactionSnapshotComponent>(body);

        if (!record.HadComponent)
        {
            RemComp<NpcFactionMemberComponent>(body);
            return;
        }

        EnsureComp<NpcFactionMemberComponent>(body);
        _factions.ClearFactions(body, false);
        _factions.AddFactions(body, record.Factions.ToHashSet());
    }

    private void RemoveAllObjectives(EntityUid mindId, MindComponent mind, IReadOnlyCollection<ProtoId<AntagPrototype>> removedRoles)
    {
        if (!CanRemoveObjectives(mindId) || mind.Objectives.Count == 0)
            return;

        _adminLogger.Add(LogType.Mind, LogImpact.Low,
            $"All objectives removed from the mind of {_minds.MindOwnerLoggingString(mind)} because antagonist roles {string.Join(", ", removedRoles.Select(role => role.Id))} were removed");

        for (var i = mind.Objectives.Count - 1; i >= 0; i--)
        {
            if (!_minds.TryRemoveObjective(mindId, mind, i))
                break;
        }
    }

    private void RemoveRoleObjectives(EntityUid mindId, MindComponent mind, IReadOnlyCollection<ProtoId<AntagPrototype>> removedRoles)
    {
        if (!CanRemoveObjectives(mindId) || mind.Objectives.Count == 0)
            return;

        var prototypes = GetRemovedRoleObjectives(mindId, removedRoles);
        if (prototypes.Count == 0)
            return;

        for (var i = mind.Objectives.Count - 1; i >= 0; i--)
        {
            if (MetaData(mind.Objectives[i]).EntityPrototype?.ID is not { } prototype || !prototypes.Contains(prototype))
                continue;

            if (!_minds.TryRemoveObjective(mindId, mind, i))
                break;
        }
    }

    private HashSet<string> GetRemovedRoleObjectives(EntityUid mindId, IReadOnlyCollection<ProtoId<AntagPrototype>> removedRoles)
    {
        var prototypes = new HashSet<string>();
        var query = EntityQueryEnumerator<AntagObjectivesComponent, AntagSelectionComponent>();
        while (query.MoveNext(out _, out var objectives, out var selection))
        {
            if (!selection.AssignedMinds.Any(assigned => assigned.Item1 == mindId))
                continue;

            if (!SelectionGrantsAnyRole(selection, removedRoles))
                continue;

            foreach (var objective in objectives.Objectives)
                prototypes.Add(objective.Id);
        }

        return prototypes;
    }

    private bool SelectionGrantsAnyRole(AntagSelectionComponent selection, IReadOnlyCollection<ProtoId<AntagPrototype>> roles)
    {
        foreach (var definition in selection.Definitions)
        {
            foreach (var role in roles)
            {
                if (definition.PrefRoles.Contains(role) || definition.FallbackRoles.Contains(role))
                    return true;

                if (definition.MindRoles is null)
                    continue;

                foreach (var mindRole in definition.MindRoles)
                {
                    if (ProtoMan.TryIndex(mindRole, out EntityPrototype? prototype)
                        && prototype.TryComp<MindRoleComponent>(out var mindRoleComp, Factory)
                        && mindRoleComp.AntagPrototype is { } roleAntag
                        && roleAntag == role)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool CanRemoveObjectives(EntityUid mindId)
    {
        if (GetMindBody(mindId) is not { } body)
            return false;

        return !HasComp<GhostComponent>(body);
    }

    private void OnRoleAdded(RoleAddedEvent args)
    {
        if (TerminatingOrDeleted(args.MindId) || !TryComp<MindComponent>(args.MindId, out var mind))
            return;

        var tracking = EnsureComp<AntagRoleTrackingComponent>(args.MindId);
        tracking.KnownRoles = GetAntagRoles(mind);
    }

    private void OnRoleRemoved(RoleRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.MindId) || !TryComp<MindComponent>(args.MindId, out var mind))
            return;

        TrackRemovedRoles(args.MindId, mind);
    }

    private void TrackRemovedRoles(EntityUid mindId, MindComponent mind)
    {
        var current = GetAntagRoles(mind);
        var tracking = EnsureComp<AntagRoleTrackingComponent>(mindId);
        var unmatched = new List<ProtoId<AntagPrototype>>(current);
        var removed = new List<ProtoId<AntagPrototype>>();

        foreach (var role in tracking.KnownRoles)
        {
            var index = unmatched.IndexOf(role);
            if (index >= 0)
            {
                unmatched.RemoveAt(index);
                continue;
            }

            removed.Add(role);
        }

        tracking.KnownRoles = current;

        if (removed.Count == 0)
            return;

        var pending = EnsureComp<AntagRoleCleanupPendingComponent>(mindId);
        pending.RemovedRoles.AddRange(removed);
    }

    private IEnumerable<EntityUid> EnumerateCarried(EntityUid body)
    {
        if (_inventory.TryGetContainerSlotEnumerator(body, out var slots))
        {
            while (slots.MoveNext(out var slot))
            {
                if (slot.ContainedEntity is { } item)
                    yield return item;
            }
        }

        foreach (var item in _hands.EnumerateHeld(body))
            yield return item;

        if (TryComp<ImplantedComponent>(body, out var implanted) && implanted.ImplantContainer != null)
        {
            foreach (var implant in implanted.ImplantContainer.ContainedEntities)
                yield return implant;
        }
    }
}
