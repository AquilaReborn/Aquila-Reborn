using Content.Server.Antag;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Administration.AntagMenu.Systems;

/// <summary>
/// Выдаёт антагонистскую роль тем же путём, что и игровое правило, и собирает список целей, подходящих разуму.
/// </summary>
public sealed class AdminAntagMenuSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;

    /// <summary>
    /// Пытается выдать разуму роль так же, как это делает правило: через активное правило или через его запуск.
    /// </summary>
    /// <param name="mind">Разум игрока.</param>
    /// <param name="antag">Прототип антагониста.</param>
    /// <returns>True, если роль выдана.</returns>
    public bool TryGrantRole(Entity<MindComponent> mind, ProtoId<AntagPrototype> antag)
    {
        if (!_prototypes.HasIndex(antag) || HasAntagRole(mind.Comp, antag.Id))
            return false;

        if (mind.Comp.UserId is not { } userId
            || !_players.TryGetSessionById(userId, out var session)
            || session.AttachedEntity == null)
        {
            return false;
        }

        return _antag.TryForceMakeAntag(session, antag);
    }

    /// <summary>
    /// Собирает прототипы целей, которые можно выдать этому разуму прямо сейчас.
    /// </summary>
    /// <param name="mindId">Сущность разума.</param>
    /// <param name="mind">Компонент разума.</param>
    /// <returns>Отсортированный список прототипов целей.</returns>
    public List<string> GetAssignableObjectives(EntityUid mindId, MindComponent mind)
    {
        var result = new List<string>();

        foreach (var prototype in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (!prototype.TryComp<ObjectiveComponent>(out _, EntityManager.ComponentFactory))
                continue;

            var objective = Spawn(prototype.ID, MapCoordinates.Nullspace);
            try
            {
                if (_objectives.CanBeAssigned(objective, mindId, mind))
                    result.Add(prototype.ID);
            }
            catch (Exception exception)
            {
                Log.Warning($"Failed to check objective {prototype.ID} for mind {mindId}: {exception}");
            }
            finally
            {
                QueueDel(objective);
            }
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private bool HasAntagRole(MindComponent mind, string antag)
    {
        var container = mind.MindRoleContainer;
        if (container == null)
            return false;

        foreach (var roleEntity in container.ContainedEntities)
        {
            if (TryComp<MindRoleComponent>(roleEntity, out var role) && role.AntagPrototype?.Id == antag)
                return true;
        }

        return false;
    }
}
