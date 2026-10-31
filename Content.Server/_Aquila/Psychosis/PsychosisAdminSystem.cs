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
        var friend = HasComp<ImaginaryFriendOwnerComponent>(target);

        if (!split)
        {
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString("psychosis-admin-verb-split"),
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
                Text = Loc.GetString("psychosis-admin-verb-friend"),
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
            Text = Loc.GetString("psychosis-admin-verb-cure"),
            Category = VerbCategory.Admin,
            Impact = LogImpact.Medium,
            Act = () =>
            {
                RemCompDeferred<SplitPersonalityComponent>(target);
                RemCompDeferred<ImaginaryFriendOwnerComponent>(target);
                _adminLog.Add(LogType.Action, LogImpact.Medium, $"{admin.Name} cured the psychosis of {ToPrettyString(target)}");
            },
        });
    }
}
