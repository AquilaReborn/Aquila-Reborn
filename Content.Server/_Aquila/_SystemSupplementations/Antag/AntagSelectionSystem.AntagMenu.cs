using Content.Server.Antag.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag;

public sealed partial class AntagSelectionSystem
{
    /// <summary>
    /// Выдаёт игроку роль антагониста через уже запущенное правило, а если подходящего правила нет - запускает его из прототипа.
    /// </summary>
    /// <param name="session">Сессия игрока.</param>
    /// <param name="antag">Прототип антагониста.</param>
    /// <returns>True, если роль выдана.</returns>
    public bool TryForceMakeAntag(ICommonSession session, ProtoId<AntagPrototype> antag)
    {
        if (TryForceMakeAntagFromActiveRule(session, antag))
            return true;

        return TryForceMakeAntagFromRulePrototype(session, antag);
    }

    private bool TryForceMakeAntagFromActiveRule(ICommonSession session, ProtoId<AntagPrototype> antag)
    {
        var query = EntityQueryEnumerator<AntagSelectionComponent>();
        while (query.MoveNext(out var rule, out var selection))
        {
            if (!HasComp<ActiveGameRuleComponent>(rule))
                continue;

            if (!TryFindAntagDefinition(selection, antag, out var definition))
                continue;

            MakeAntag((rule, selection), session, definition, ignoreSpawner: true);
            return true;
        }

        return false;
    }

    private bool TryForceMakeAntagFromRulePrototype(ICommonSession session, ProtoId<AntagPrototype> antag)
    {
        foreach (var prototype in ProtoMan.EnumeratePrototypes<EntityPrototype>())
        {
            if (!prototype.TryComp<AntagSelectionComponent>(out var selection, Factory)
                || !TryFindAntagDefinition(selection, antag, out _))
            {
                continue;
            }

            var rule = GameTicker.AddGameRule(prototype.ID);
            if (!TryComp<AntagSelectionComponent>(rule, out var ruleSelection)
                || !TryFindAntagDefinition(ruleSelection, antag, out var definition))
            {
                QueueDel(rule);
                return false;
            }

            ruleSelection.AssignmentComplete = true;
            RemComp<LoadMapRuleComponent>(rule);
            GameTicker.StartGameRule(rule);
            MakeAntag((rule, ruleSelection), session, definition, ignoreSpawner: true);
            return true;
        }

        return false;
    }

    private bool TryFindAntagDefinition(
        AntagSelectionComponent selection,
        ProtoId<AntagPrototype> antag,
        out AntagSelectionDefinition definition)
    {
        foreach (var candidate in selection.Definitions)
        {
            if (!AntagDefinitionMatches(candidate, antag))
                continue;

            definition = candidate;
            return true;
        }

        definition = default;
        return false;
    }

    private bool AntagDefinitionMatches(AntagSelectionDefinition definition, ProtoId<AntagPrototype> antag)
    {
        if (definition.PrefRoles.Contains(antag) || definition.FallbackRoles.Contains(antag))
            return true;

        if (definition.MindRoles == null)
            return false;

        foreach (var roleId in definition.MindRoles)
        {
            if (!ProtoMan.TryIndex<EntityPrototype>(roleId, out var rolePrototype)
                || !rolePrototype.TryComp<MindRoleComponent>(out var role, Factory))
            {
                continue;
            }

            if (role.AntagPrototype is { } roleAntag && roleAntag == antag)
                return true;
        }

        return false;
    }
}
