using Content.Server._Aquila.Psychosis;
using Content.Server._Aquila.Psychosis.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Aquila.StationEvents;

public sealed class PsychicAnomalyRule : StationEventSystem<PsychicAnomalyRuleComponent>
{
    private static readonly LocId Announcement = "station-event-psychic-anomaly-announcement";
    private static readonly SoundSpecifier AnnouncementSound = new SoundPathSpecifier("/Audio/Announcements/attention.ogg");

    [Dependency] private readonly ImaginaryFriendSystem _friend = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SplitPersonalitySystem _split = default!;

    protected override void Started(EntityUid uid, PsychicAnomalyRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var candidates = new List<EntityUid>();
        var query = EntityQueryEnumerator<MindContainerComponent, HumanoidAppearanceComponent, ActorComponent>();
        while (query.MoveNext(out var ent, out _, out _, out _))
        {
            if (!_mobState.IsAlive(ent)
                || HasComp<SplitPersonalityComponent>(ent)
                || HasComp<ImaginaryFriendOwnerComponent>(ent))
                continue;

            candidates.Add(ent);
        }

        RobustRandom.Shuffle(candidates);
        var count = Math.Min(candidates.Count, RobustRandom.Next(component.MinTargets, component.MaxTargets + 1));

        for (var i = 0; i < count; i++)
        {
            if (RobustRandom.Prob(component.FriendChance))
                _friend.TryAfflict(candidates[i]);
            else
                _split.TryAfflict(candidates[i]);
        }

        if (count > 0)
            Announce();
    }

    /// <summary>
    /// Объявляет о психической аномалии всем игрокам в раунде.
    /// </summary>
    public void Announce()
    {
        var filter = Filter.Empty().AddWhere(GameTicker.UserHasJoinedGame);
        ChatSystem.DispatchFilteredAnnouncement(filter, Loc.GetString(Announcement), playSound: false, colorOverride: Color.Gold);
        ChatSystem.SendGlobalSound(AnnouncementSound, filter);
    }
}
