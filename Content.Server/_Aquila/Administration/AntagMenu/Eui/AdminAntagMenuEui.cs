using System.Linq;
using Content.Server._Aquila.Administration.AntagMenu.Systems;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared._Aquila.Administration.AntagMenu.DataDefinitions;
using Content.Shared._Aquila.Administration.AntagMenu.Eui;
using Content.Shared._Aquila.Administration.AntagMenu.Messages;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Eui;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Server.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Administration.AntagMenu.Eui;

/// <summary>
/// Серверное меню администратора для просмотра антагонистов, их ролей и целей.
/// </summary>
public sealed class AdminAntagMenuEui : BaseEui
{
    private static readonly LocId ErrorObjectivePrototype = "admin-antag-menu-error-objective-prototype";
    private static readonly LocId ErrorAddObjective = "admin-antag-menu-error-add-objective";
    private static readonly LocId ErrorNoCounter = "admin-antag-menu-error-no-counter";
    private static readonly LocId ErrorRolePrototype = "admin-antag-menu-error-role-prototype";

    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly Dictionary<NetEntity, List<string>> _assignableObjectives = new();
    private readonly AdminAntagMenuSystem _menu;
    private readonly SharedMindSystem _mind;
    private readonly NumberObjectiveSystem _numbers;
    private readonly SharedObjectivesSystem _objectives;
    private readonly SharedRoleSystem _roles;
    private readonly ISawmill _sawmill;

    /// <summary>
    /// Создаёт интерфейс и подключает системы, нужные для работы с разумами и целями.
    /// </summary>
    public AdminAntagMenuEui()
    {
        IoCManager.InjectDependencies(this);

        _menu = _entity.System<AdminAntagMenuSystem>();
        _mind = _entity.System<SharedMindSystem>();
        _numbers = _entity.System<NumberObjectiveSystem>();
        _objectives = _entity.System<SharedObjectivesSystem>();
        _roles = _entity.System<SharedRoleSystem>();
        _sawmill = _logManager.GetSawmill("admin.antagmenu");
    }

    /// <inheritdoc/>
    public override void Opened()
    {
        base.Opened();

        _admins.OnPermsChanged += OnPermsChanged;
        StateDirty();
    }

    /// <inheritdoc/>
    public override void Closed()
    {
        base.Closed();

        _admins.OnPermsChanged -= OnPermsChanged;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !_admins.HasAdminFlag(Player, AdminFlags.Admin))
            Close();
    }

    /// <inheritdoc/>
    public override EuiStateBase GetNewState()
    {
        var state = new AdminAntagMenuEuiState
        {
            GrantableAntags = GetGrantableAntags(),
        };

        foreach (var (mindId, component) in _entity.GetAllComponents(typeof(MindComponent), includePaused: true))
        {
            if (component is not MindComponent mind)
                continue;

            if (_entity.Deleted(mindId))
                continue;

            try
            {
                var roles = GetAntagRoles(mind);
                if (mind.UserId == null && roles.Count == 0)
                    continue;

                state.Minds.Add(new AdminAntagMenuMindData
                {
                    Mind = _entity.GetNetEntity(mindId),
                    Name = GetMindName(mind),
                    Username = GetMindUsername(mind),
                    Roles = roles,
                    Objectives = GetObjectives(mindId, mind),
                });
            }
            catch (Exception exception)
            {
                _sawmill.Error($"Failed to build antagonist menu state for mind {mindId}: {exception}");
            }
        }

        state.Minds.Sort(CompareMinds);
        return state;
    }

    /// <inheritdoc/>
    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is CloseEuiMessage)
            return;

        if (!_admins.HasAdminFlag(Player, AdminFlags.Admin))
        {
            Close();
            return;
        }

        switch (msg)
        {
            case AdminAntagMenuRequestObjectivesMessage request:
                SendAssignableObjectives(request.Mind);
                return;
            case AdminAntagMenuRefreshMessage:
                break;
            case AdminAntagMenuAddObjectiveMessage add:
                AddObjective(add.Mind, add.Prototype);
                _assignableObjectives.Remove(add.Mind);
                break;
            case AdminAntagMenuRemoveObjectiveMessage remove:
                RemoveObjective(remove.Mind, remove.Objective);
                _assignableObjectives.Remove(remove.Mind);
                break;
            case AdminAntagMenuSetObjectiveTargetMessage target:
                SetObjectiveTarget(target.Mind, target.Objective, target.Value);
                break;
            case AdminAntagMenuRemoveAllObjectivesMessage removeAll:
                RemoveAllObjectives(removeAll.Mind);
                _assignableObjectives.Remove(removeAll.Mind);
                break;
            case AdminAntagMenuGrantRoleMessage grant:
                GrantRole(grant.Mind, grant.Antag);
                _assignableObjectives.Remove(grant.Mind);
                break;
            case AdminAntagMenuRemoveRoleMessage removeRole:
                RemoveRole(removeRole.Mind, removeRole.Antag);
                _assignableObjectives.Remove(removeRole.Mind);
                break;
            default:
                return;
        }

        StateDirty();
    }

    private void SendAssignableObjectives(NetEntity netMind)
    {
        if (GetMind(netMind) is not { } mind)
            return;

        if (!_assignableObjectives.TryGetValue(netMind, out var prototypes))
        {
            prototypes = _menu.GetAssignableObjectives(mind.Owner, mind.Comp);
            _assignableObjectives[netMind] = prototypes;
        }

        SendMessage(new AdminAntagMenuObjectivesMessage(netMind, prototypes));
    }

    private List<string> GetGrantableAntags()
    {
        var antags = new HashSet<ProtoId<AntagPrototype>>();

        foreach (var prototype in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (!prototype.TryComp<MindRoleComponent>(out var role, _entity.ComponentFactory)
                || role.AntagPrototype is not { } antag
                || !_prototypes.HasIndex<AntagPrototype>(antag))
            {
                continue;
            }

            antags.Add(antag);
        }

        var antagList = _menu.FilterGrantableAntags(antags).Select(antag => antag.Id).ToList();
        antagList.Sort(StringComparer.Ordinal);
        return antagList;
    }

    private List<string> GetAntagRoles(MindComponent mind)
    {
        var roles = new List<string>();
        var seen = new HashSet<string>();

        var container = mind.MindRoleContainer;
        if (container == null)
            return roles;

        foreach (var roleEntity in container.ContainedEntities)
        {
            if (!_entity.TryGetComponent<MindRoleComponent>(roleEntity, out var role)
                || role.AntagPrototype is not { } antag)
            {
                continue;
            }

            if (_prototypes.HasIndex<AntagPrototype>(antag) && seen.Add(antag.Id))
                roles.Add(antag.Id);
        }

        return roles;
    }

    private string GetMindName(MindComponent mind)
    {
        if (!string.IsNullOrWhiteSpace(mind.CharacterName))
            return mind.CharacterName;

        if (mind.OwnedEntity is { } owned && _entity.EntityExists(owned) && !_entity.Deleted(owned))
            return _entity.GetComponent<MetaDataComponent>(owned).EntityName;

        return string.Empty;
    }

    private string GetMindUsername(MindComponent mind)
    {
        return mind.UserId is { } userId && _players.TryGetSessionById(userId, out var session)
            ? session.Name
            : string.Empty;
    }

    private List<AdminAntagMenuObjectiveData> GetObjectives(EntityUid mindId, MindComponent mind)
    {
        var objectives = new List<AdminAntagMenuObjectiveData>();

        foreach (var objective in mind.Objectives)
        {
            if (!_entity.EntityExists(objective)
                || _entity.Deleted(objective)
                || !_entity.TryGetComponent<ObjectiveComponent>(objective, out _))
            {
                continue;
            }

            var meta = _entity.GetComponent<MetaDataComponent>(objective);
            var info = _objectives.GetInfo(objective, mindId, mind);
            var title = info?.Title ?? meta.EntityName;
            var description = info?.Description ?? meta.EntityDescription;
            var progress = info?.Progress ?? 0f;

            int? target = null;
            int? current = null;
            if (_entity.TryGetComponent<NumberObjectiveComponent>(objective, out var number))
            {
                target = number.Target;
                current = target > 0
                    ? Math.Clamp((int) MathF.Round(progress * target.Value), 0, target.Value)
                    : 0;
            }

            objectives.Add(new AdminAntagMenuObjectiveData
            {
                Objective = _entity.GetNetEntity(objective),
                Prototype = meta.EntityPrototype?.ID ?? string.Empty,
                Title = title,
                Description = description,
                Progress = progress,
                Target = target,
                Current = current,
            });
        }

        return objectives;
    }

    private static int CompareMinds(AdminAntagMenuMindData a, AdminAntagMenuMindData b)
    {
        var aAntag = a.Roles.Count > 0;
        var bAntag = b.Roles.Count > 0;
        if (aAntag != bAntag)
            return aAntag ? -1 : 1;

        return string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
    }

    private Entity<MindComponent>? GetMind(NetEntity netMind)
    {
        if (!_entity.TryGetEntity(netMind, out var uid)
            || _entity.Deleted(uid.Value)
            || !_entity.TryGetComponent<MindComponent>(uid.Value, out var mind))
        {
            return null;
        }

        return (uid.Value, mind);
    }

    private bool TryGetObjective(Entity<MindComponent> mind, NetEntity netObjective, out EntityUid objective)
    {
        objective = default;
        if (!_entity.TryGetEntity(netObjective, out var uid)
            || _entity.Deleted(uid.Value)
            || !mind.Comp.Objectives.Contains(uid.Value)
            || !_entity.TryGetComponent<ObjectiveComponent>(uid.Value, out _))
        {
            return false;
        }

        objective = uid.Value;
        return true;
    }

    private void AddObjective(NetEntity netMind, string prototype)
    {
        if (GetMind(netMind) is not { } mind)
            return;

        if (!_prototypes.TryIndex<EntityPrototype>(prototype, out var entityPrototype)
            || !entityPrototype.TryComp<ObjectiveComponent>(out _, _entity.ComponentFactory))
        {
            SendError(ErrorObjectivePrototype);
            return;
        }

        if (!_mind.TryAddObjective(mind.Owner, mind.Comp, prototype))
        {
            SendError(ErrorAddObjective);
            return;
        }

        _adminLog.Add(LogType.Mind, LogImpact.Medium,
            $"{Player:actor} added objective {prototype} to the mind of {_mind.MindOwnerLoggingString(mind.Comp)}");
    }

    private void RemoveObjective(NetEntity netMind, NetEntity netObjective)
    {
        if (GetMind(netMind) is not { } mind || !TryGetObjective(mind, netObjective, out var objective))
            return;

        var index = mind.Comp.Objectives.IndexOf(objective);
        if (index < 0 || !_mind.TryRemoveObjective(mind.Owner, mind.Comp, index))
            return;

        _adminLog.Add(LogType.Mind, LogImpact.Medium,
            $"{Player:actor} removed objective {_entity.ToPrettyString(objective):subject} from the mind of {_mind.MindOwnerLoggingString(mind.Comp)}");
    }

    private void SetObjectiveTarget(NetEntity netMind, NetEntity netObjective, int value)
    {
        if (GetMind(netMind) is not { } mind || !TryGetObjective(mind, netObjective, out var objective))
            return;

        if (!_entity.TryGetComponent<NumberObjectiveComponent>(objective, out _))
        {
            SendError(ErrorNoCounter);
            return;
        }

        _numbers.SetTarget(objective, value);
        _adminLog.Add(LogType.Mind, LogImpact.Low,
            $"{Player:actor} changed the target of {_entity.ToPrettyString(objective):subject} to {value}");
    }

    private void RemoveAllObjectives(NetEntity netMind)
    {
        if (GetMind(netMind) is not { } mind || mind.Comp.Objectives.Count == 0)
            return;

        var removed = false;
        for (var i = mind.Comp.Objectives.Count - 1; i >= 0; i--)
        {
            if (!_mind.TryRemoveObjective(mind.Owner, mind.Comp, i))
                break;

            removed = true;
        }

        if (!removed)
            return;

        _adminLog.Add(LogType.Mind, LogImpact.Medium,
            $"{Player:actor} removed all objectives from the mind of {_mind.MindOwnerLoggingString(mind.Comp)}");
    }

    private void GrantRole(NetEntity netMind, string antag)
    {
        if (GetMind(netMind) is not { } mind)
            return;

        if (!_prototypes.TryIndex<AntagPrototype>(antag, out _) || HasAntagRole(mind.Comp, antag))
            return;

        if (!_menu.TryGrantRole(mind, new ProtoId<AntagPrototype>(antag)))
        {
            SendError(ErrorRolePrototype);
            return;
        }

        _adminLog.Add(LogType.Mind, LogImpact.Medium,
            $"{Player:actor} granted antagonist role {antag} to the mind of {_mind.MindOwnerLoggingString(mind.Comp)}");
    }

    private bool HasAntagRole(MindComponent mind, string antag)
    {
        var container = mind.MindRoleContainer;
        if (container == null)
            return false;

        foreach (var roleEntity in container.ContainedEntities)
        {
            if (_entity.TryGetComponent<MindRoleComponent>(roleEntity, out var role)
                && role.AntagPrototype?.Id == antag)
            {
                return true;
            }
        }

        return false;
    }

    private void RemoveRole(NetEntity netMind, string antag)
    {
        if (GetMind(netMind) is not { } mind)
            return;

        var container = mind.Comp.MindRoleContainer;
        if (container == null)
            return;

        var prototypes = new HashSet<string>();
        foreach (var roleEntity in container.ContainedEntities)
        {
            if (!_entity.TryGetComponent<MindRoleComponent>(roleEntity, out var role)
                || role.AntagPrototype?.Id != antag)
            {
                continue;
            }

            if (_entity.GetComponent<MetaDataComponent>(roleEntity).EntityPrototype?.ID is { } prototype)
                prototypes.Add(prototype);
        }

        if (prototypes.Count == 0)
            return;

        var removed = false;
        foreach (var prototype in prototypes)
            removed |= _roles.MindRemoveRole((mind.Owner, mind.Comp), new EntProtoId<MindRoleComponent>(prototype));

        if (!removed)
            return;

        _adminLog.Add(LogType.Mind, LogImpact.Medium,
            $"{Player:actor} removed antagonist role {antag} from the mind of {_mind.MindOwnerLoggingString(mind.Comp)}");
    }

    private void SendError(LocId messageKey)
    {
        SendMessage(new AdminAntagMenuErrorMessage(messageKey));
    }
}
