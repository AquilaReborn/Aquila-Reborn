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

    /// <summary>
    /// Оставляет только тех антагонистов, для которых хотя бы в одном прототипе правила есть подходящее определение.
    /// </summary>
    /// <param name="candidates">Антагонисты, среди которых идёт поиск.</param>
    /// <returns>Антагонисты, которых можно выдать через правило.</returns>
    public HashSet<ProtoId<AntagPrototype>> FilterGrantableAntags(IReadOnlySet<ProtoId<AntagPrototype>> candidates)
    {
        var grantable = new HashSet<ProtoId<AntagPrototype>>();
        if (candidates.Count == 0)
            return grantable;

        foreach (var prototype in ProtoMan.EnumeratePrototypes<EntityPrototype>())
        {
            if (!prototype.TryComp<AntagSelectionComponent>(out var selection, Factory))
                continue;

            foreach (var antag in candidates)
            {
                if (grantable.Contains(antag))
                    continue;

                if (TryFindAntagDefinition(selection, antag, out _))
                    grantable.Add(antag);
            }

            if (grantable.Count == candidates.Count)
                break;
        }

        return grantable;
    }

    private bool TryForceMakeAntagFromActiveRule(ICommonSession session, ProtoId<AntagPrototype> antag)
    {
        var query = EntityQueryEnumerator<AntagSelectionComponent>();
        while (query.MoveNext(out var rule, out var selection))
        {
            if (!HasComp<ActiveGameRuleComponent>(rule))
                continue;

            if (!TryFindExactAntagDefinition(selection, antag, out var definition)
                && !TryFindAntagDefinition(selection, antag, out definition))
            {
                continue;
            }

            return TryMakeAntag((rule, selection), session, definition, ignoreSpawner: true, checkPref: false);
        }

        return false;
    }

    private bool TryForceMakeAntagFromRulePrototype(ICommonSession session, ProtoId<AntagPrototype> antag)
    {
        var exactFound = false;
        foreach (var prototype in ProtoMan.EnumeratePrototypes<EntityPrototype>())
        {
            if (!prototype.TryComp<AntagSelectionComponent>(out var selection, Factory)
                || !TryFindExactAntagDefinition(selection, antag, out _))
            {
                continue;
            }

            exactFound = true;
            if (TryStartRuleAndMakeAntag(prototype.ID, session, antag, exactMatch: true))
                return true;
        }

        if (exactFound)
            return false;

        foreach (var prototype in ProtoMan.EnumeratePrototypes<EntityPrototype>())
        {
            if (!prototype.TryComp<AntagSelectionComponent>(out var selection, Factory)
                || !TryFindAntagDefinition(selection, antag, out _))
            {
                continue;
            }

            if (TryStartRuleAndMakeAntag(prototype.ID, session, antag, exactMatch: false))
                return true;
        }

        return false;
    }

    private bool TryStartRuleAndMakeAntag(
        string rulePrototype,
        ICommonSession session,
        ProtoId<AntagPrototype> antag,
        bool exactMatch)
    {
        var rule = GameTicker.AddGameRule(rulePrototype);
        if (!TryComp<AntagSelectionComponent>(rule, out var selection))
        {
            QueueDel(rule);
            return false;
        }

        AntagSelectionDefinition definition;
        var found = exactMatch
            ? TryFindExactAntagDefinition(selection, antag, out definition)
            : TryFindAntagDefinition(selection, antag, out definition);

        if (!found)
        {
            QueueDel(rule);
            return false;
        }

        selection.AssignmentComplete = true;
        RemComp<LoadMapRuleComponent>(rule);
        GameTicker.StartGameRule(rule);

        if (!TryMakeAntag((rule, selection), session, definition, ignoreSpawner: true, checkPref: false))
        {
            QueueDel(rule);
            return false;
        }

        return true;
    }

    private bool TryFindExactAntagDefinition(
        AntagSelectionComponent selection,
        ProtoId<AntagPrototype> antag,
        out AntagSelectionDefinition definition)
    {
        foreach (var candidate in selection.Definitions)
        {
            if (!HasAntagMindRole(candidate, antag))
                continue;

            definition = candidate;
            return true;
        }

        definition = default;
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

        return HasAntagMindRole(definition, antag);
    }

    private bool HasAntagMindRole(AntagSelectionDefinition definition, ProtoId<AntagPrototype> antag)
    {
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
