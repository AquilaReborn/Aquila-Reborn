using System.Diagnostics.CodeAnalysis;
using Content.Server._Aquila.Antags.RoleCleanup.Events;
using Content.Server._Aquila.Antags.RoleCleanup.Systems;
using Content.Server._Aquila.Heretic.Moon.Components;
using Content.Server.Antag;
using Content.Server.Heretic.EntitySystems;
using Content.Server.Roles;
using Content.Server.Traits.Assorted;
using Content.Shared._Aquila.Heretic.Moon.Components;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.Traits.Assorted;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Aquila.Heretic.Moon.Systems;

public sealed partial class MoonAscensionSystem
{
    [Dependency] private readonly AntagRoleCleanupSystem _roleCleanup = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly GhoulSystem _ghoul = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly ParacusiaSystem _paracusia = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private static readonly ProtoId<AntagPrototype> LunaticAntag = "MoonLunatic";
    private static readonly EntProtoId LunaticRule = "MoonLunaticRule";
    private static readonly EntProtoId LunaticInsanityStatus = "StatusEffectMoonLunatic";
    private static readonly LocId LunaticWelcomeMessage = "moon-lunatic-welcome";
    private static readonly LocId LunaticBriefingMessage = "moon-lunatic-briefing";
    private static readonly LocId LunaticBriefingNoNameMessage = "moon-lunatic-briefing-noname";
    private static readonly string NeckSlot = "neck";

    private void ConvertLunatics(Entity<MoonAscensionComponent> aura,
        EntityUid heretic,
        TimeSpan now,
        HashSet<Entity<MobStateComponent>> targets)
    {
        if (now < aura.Comp.NextConversion)
            return;

        var candidates = new List<(EntityUid Target, ICommonSession Session)>();
        foreach (var target in targets)
        {
            if (CanConvertLunatic(target.Owner, target.Comp, aura.Comp, out var session))
                candidates.Add((target.Owner, session));
        }

        if (candidates.Count == 0)
            return;

        var converted = 0;

        while (converted < aura.Comp.LunaticsPerConversion && candidates.Count > 0)
        {
            var pick = _random.Next(candidates.Count);
            var (target, session) = candidates[pick];
            candidates.RemoveAt(pick);

            if (!DoConvertLunatic(aura, heretic, target, session))
                continue;

            converted++;
        }

        aura.Comp.NextConversion = now + aura.Comp.ConversionInterval;
    }

    private bool CanConvertLunatic(EntityUid target,
        MobStateComponent state,
        MoonAscensionComponent aura,
        [NotNullWhen(true)] out ICommonSession? session)
    {
        session = null;

        if (!_mobState.IsAlive(target, state)
            || _heretic.IsHereticOrGhoul(target)
            || HasComp<HereticMinionComponent>(target)
            || HasComp<MoonLunaticComponent>(target)
            || HasComp<MindShieldComponent>(target))
            return false;

        if (!_sanity.IsInsane(target, aura.LunaticSanityThreshold))
            return false;

        if (!_mind.TryGetMind(target, out var mindId, out var mind) || mind.UserId is not { } userId)
            return false;

        if (!_player.TryGetSessionById(userId, out var found))
            return false;

        if (_role.MindIsAntagonist(mindId))
            return false;

        session = found;
        return true;
    }

    private bool DoConvertLunatic(Entity<MoonAscensionComponent> aura, EntityUid heretic, EntityUid target, ICommonSession session)
    {
        var rule = _antag.ForceGetGameRuleEnt<MoonLunaticRuleComponent>(LunaticRule.Id);
        if (rule.Comp.Definitions.Count == 0)
            return false;

        if (!_antag.TryMakeAntag(rule, session, rule.Comp.Definitions[^1], ignoreSpawner: true, checkPref: false))
            return false;

        var member = EnsureComp<NpcFactionMemberComponent>(target);
        EnsureComp<MoonLunaticComponent>(target);

        _faction.ClearFactions((target, member), false);
        _faction.AddFaction((target, member), HereticSystem.HereticFactionId);
        _ghoul.SetBoundHeretic((target, (HereticMinionComponent?) null), heretic);

        _status.TryAddStatusEffect(target, LunaticInsanityStatus, out _);
        ApplyLunaticLaughter(target, aura.Comp);

        SpawnAtPosition(aura.Comp.Effect, Transform(target).Coordinates);

        var amulet = SpawnNextToOrDrop(aura.Comp.LunaticAmulet, target);
        if (!_inventory.TryEquip(target, amulet, NeckSlot, silent: true))
            _hands.TryPickupAnyHand(target, amulet, checkActionBlocker: false);

        _antag.SendBriefing(session,
            Loc.GetString(LunaticWelcomeMessage, ("ent", Identity.Entity(heretic, EntityManager))),
            Color.MediumPurple,
            aura.Comp.LunaticSound);

        return true;
    }

    private void ApplyLunaticLaughter(EntityUid target, MoonAscensionComponent aura)
    {
        if (aura.LunaticSound is not { } sound)
            return;

        EnsureComp<ParacusiaComponent>(target);

        _paracusia.SetSounds(target, sound);
        _paracusia.SetTime(target, aura.LunaticLaughterMinInterval, aura.LunaticLaughterMaxInterval);
        _paracusia.SetDistance(target, aura.LunaticLaughterDistance);
    }

    private void OnAntagRoleCleanup(AntagRoleCleanupEvent args)
    {
        if (!args.WasRemoved(LunaticAntag) || args.HasRemainingRole(LunaticAntag) || args.Body is not { } body)
            return;

        _roleCleanup.RestoreFactions(body, args.RemainingRoles);
    }

    private void OnLunaticBriefing(Entity<MoonLunaticRoleComponent> ent, ref GetBriefingEvent args)
    {
        var body = args.Mind.Comp.OwnedEntity;

        if (body is not { } owner
            || !TryComp<HereticMinionComponent>(owner, out var minion)
            || minion.BoundHeretic is not { } master
            || !Exists(master))
        {
            args.Append(Loc.GetString(LunaticBriefingNoNameMessage));
            return;
        }

        args.Append(Loc.GetString(LunaticBriefingMessage, ("ent", Identity.Entity(master, EntityManager))));
    }
}
