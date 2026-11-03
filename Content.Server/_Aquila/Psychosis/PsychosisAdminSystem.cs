using Content.Server._Aquila.Psychosis.Components;
using Content.Server._Aquila.StationEvents;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server._Aquila.Psychosis;

/// <summary>
/// Админские действия: выдать игроку раздвоение личности или воображаемого друга и вылечить их.
/// </summary>
public sealed class PsychosisAdminSystem : EntitySystem
{
    private static readonly LocId PsychosisAdminVerbSplit = "psychosis-admin-verb-split";
    private static readonly LocId PsychosisAdminVerbFriend = "psychosis-admin-verb-friend";
    private static readonly LocId PsychosisAdminVerbCure = "psychosis-admin-verb-cure";

    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly ImaginaryFriendSystem _friend = default!;
    [Dependency] private readonly PsychicAnomalyRule _anomaly = default!;
    [Dependency] private readonly SplitPersonalitySystem _split = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor)
            || !_admin.HasAdminFlag(actor.PlayerSession, AdminFlags.Fun)
            || !HasComp<MindContainerComponent>(args.Target)
            || !HasComp<HumanoidAppearanceComponent>(args.Target))
            return;

        var admin = actor.PlayerSession;
        var target = args.Target;
        var split = HasComp<SplitPersonalityComponent>(target);
        var friend = _friend.HasImaginaryFriend(target);

        if (!split)
        {
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString(PsychosisAdminVerbSplit),
                Category = VerbCategory.Admin,
                Impact = LogImpact.Medium,
                Act = () =>
                {
                    if (!_split.TryAfflict(target))
                        return;

                    _anomaly.Announce();
                    _adminLog.Add(LogType.Action, LogImpact.Medium, $"{admin.Name} gave a split personality to {ToPrettyString(target)}");
                },
            });
        }

        if (!friend)
        {
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString(PsychosisAdminVerbFriend),
                Category = VerbCategory.Admin,
                Impact = LogImpact.Medium,
                Act = () =>
                {
                    if (!_friend.TryAfflict(target))
                        return;

                    _anomaly.Announce();
                    _adminLog.Add(LogType.Action, LogImpact.Medium, $"{admin.Name} gave an imaginary friend to {ToPrettyString(target)}");
                },
            });
        }

        if (!split && !friend)
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString(PsychosisAdminVerbCure),
            Category = VerbCategory.Admin,
            Impact = LogImpact.Medium,
            Act = () =>
            {
                RemCompDeferred<SplitPersonalityComponent>(target);
                _friend.Cure(target);
                _adminLog.Add(LogType.Action, LogImpact.Medium, $"{admin.Name} cured the psychosis of {ToPrettyString(target)}");
            },
        });
    }
}
