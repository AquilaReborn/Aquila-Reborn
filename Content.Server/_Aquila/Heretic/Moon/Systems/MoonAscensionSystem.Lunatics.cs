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
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Enums;

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

        var candidates = new List<EntityUid>();
        foreach (var target in targets)
        {
            if (CanConvertLunatic((target.Owner, target.Comp), aura.Comp.LunaticSanityThreshold, out _))
                candidates.Add(target.Owner);
        }

        if (candidates.Count == 0)
            return;

        var converted = 0;

        while (converted < aura.Comp.LunaticsPerConversion && candidates.Count > 0)
        {
            var pick = _random.Next(candidates.Count);
            var target = candidates[pick];
            candidates.RemoveAt(pick);

            if (!TryConvertLunatic((target, null), heretic, aura.Comp.LunaticSanityThreshold,
                    aura.Comp.Effect, aura.Comp.LunaticAmulet, aura.Comp.LunaticSound))
                continue;

            if (_status.TryGetStatusEffect(target, LunaticInsanityStatus, out var status))
                ApplyLunaticLaughter(status.Value, aura.Comp);

            converted++;
        }

        aura.Comp.NextConversion = now + aura.Comp.ConversionInterval;
    }

    private void ConvertAscensionLunatics(Entity<MoonAscensionComponent> aura)
    {
        var eligible = new List<EntityUid>();
        foreach (var session in _player.Sessions)
        {
            if (session.AttachedEntity is not { } body)
                continue;

            if (!HasComp<MobStateComponent>(body))
                continue;

            if (!CanConvertLunatic((body, null), 0f, out _))
                continue;

            eligible.Add(body);
        }

        var candidates = new List<EntityUid>(eligible.Count);
        foreach (var body in eligible)
        {
            if (!_sanity.IsInsane(body, aura.Comp.LunaticSanityThreshold))
            {
                candidates.Add(body);
                continue;
            }

            if (!TryConvertLunatic((body, null), aura.Owner, aura.Comp.LunaticSanityThreshold,
                    aura.Comp.Effect, aura.Comp.LunaticAmulet, aura.Comp.LunaticSound))
                continue;

            if (_status.TryGetStatusEffect(body, LunaticInsanityStatus, out var status))
                ApplyLunaticLaughter(status.Value, aura.Comp);
        }

        if (candidates.Count == 0)
            return;

        var quota = (int) MathF.Ceiling(eligible.Count * Math.Clamp(aura.Comp.AscensionConvertedFraction, 0f, 1f));
        var converted = 0;

        while (converted < quota && candidates.Count > 0)
        {
            var pick = _random.Next(candidates.Count);
            var target = candidates[pick];
            candidates.RemoveAt(pick);

            if (!TryConvertLunatic((target, null), aura.Owner, 0f,
                    aura.Comp.Effect, aura.Comp.LunaticAmulet, aura.Comp.LunaticSound))
                continue;

            if (_status.TryGetStatusEffect(target, LunaticInsanityStatus, out var status))
                ApplyLunaticLaughter(status.Value, aura.Comp);

            converted++;
        }
    }

    /// <summary>
    /// Обращает живого игрока по общим правилам ауры и амулета.
    /// </summary>
    public bool TryConvertLunatic(Entity<MobStateComponent?> ent, EntityUid user, float threshold,
        EntProtoId effect, EntProtoId amulet, SoundSpecifier? sound)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, logMissing: false))
            return false;

        if (!CanConvertLunatic((ent.Owner, ent.Comp), threshold, out var session))
            return false;

        return DoConvertLunatic(user, ent.Owner, session, effect, amulet, sound);
    }

    private bool CanConvertLunatic(Entity<MobStateComponent?> ent, float threshold,
        [NotNullWhen(true)] out ICommonSession? session, bool quiet = false)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, logMissing: false))
        {
            session = null;
            return false;
        }

        session = null;
        var target = ent.Owner;

        if (TerminatingOrDeleted(target) || EntityManager.IsQueuedForDeletion(target) || !_mobState.IsAlive(target, ent.Comp)
            || _heretic.IsHereticOrGhoul(target)
            || HasComp<HereticMinionComponent>(target)
            || HasComp<MoonLunaticComponent>(target)
            || HasComp<MindShieldComponent>(target))
            return false;

        if (!_sanity.IsInsane(target, threshold))
            return false;

        if (!_mind.TryGetMind(target, out var mindId, out var mind) || mind.UserId is not { } userId ||
            mind.CurrentEntity != target || mind.OwnedEntity != target)
            return false;

        if (!_player.TryGetSessionById(userId, out var found) || found.AttachedEntity != target || found.Status != SessionStatus.InGame)
            return false;

        if (_role.MindIsAntagonist(mindId))
            return false;

        session = found;
        return true;
    }

    private bool DoConvertLunatic(EntityUid heretic, EntityUid target, ICommonSession session,
        EntProtoId effect, EntProtoId amuletPrototype, SoundSpecifier? sound)
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
        SpawnAtPosition(effect, Transform(target).Coordinates);

        if (_inventory.TryGetSlotEntity(target, NeckSlot, out _))
            _inventory.TryUnequip(target, NeckSlot, silent: true, force: true);

        var amulet = SpawnNextToOrDrop(amuletPrototype, target);
        if (!_inventory.TryEquip(target, amulet, NeckSlot, silent: true))
            _hands.TryPickupAnyHand(target, amulet, checkActionBlocker: false);

        _antag.SendBriefing(session,
            Loc.GetString(LunaticWelcomeMessage, ("ent", Identity.Entity(heretic, EntityManager))),
            Color.MediumPurple,
            sound);

        return true;
    }

    private void ApplyLunaticLaughter(EntityUid status, MoonAscensionComponent aura)
    {
        if (aura.LunaticSound is not { } sound)
            return;

        _paracusia.SetSounds(status, sound);
        _paracusia.SetTime(status, aura.LunaticLaughterMinInterval, aura.LunaticLaughterMaxInterval);
        _paracusia.SetDistance(status, aura.LunaticLaughterDistance);
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
