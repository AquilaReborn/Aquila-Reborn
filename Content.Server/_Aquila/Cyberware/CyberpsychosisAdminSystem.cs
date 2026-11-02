using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared._Aquila.Cyberware.Components;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Cyberware;

/// <summary>
/// Админские действия: выдать любую стадию киберпсихоза или вылечить его.
/// </summary>
public sealed class CyberpsychosisAdminSystem : EntitySystem
{
    private static readonly LocId CyberpsychosisAdminVerbStage = "cyberpsychosis-admin-verb-stage";
    private static readonly LocId CyberpsychosisAdminVerbCure = "cyberpsychosis-admin-verb-cure";

    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly CyberLoadSystem _cyberLoad = default!;
    [Dependency] private readonly CyberpsychosisSystem _cyberpsychosis = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor)
            || !_admin.HasAdminFlag(actor.PlayerSession, AdminFlags.Fun)
            || !HasComp<HumanoidAppearanceComponent>(args.Target)
            || !_cyberLoad.TryGetLoad(args.Target, out var load))
            return;

        var admin = actor.PlayerSession;
        var target = args.Target;
        var stages = load.Stages;

        for (var i = 0; i < stages.Count; i++)
        {
            if (!_proto.TryIndex(stages[i], out var stage))
                continue;

            var level = i + 1;
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString(CyberpsychosisAdminVerbStage, ("stage", Loc.GetString(stage.Name))),
                Category = VerbCategory.Admin,
                Impact = LogImpact.Medium,
                Act = () =>
                {
                    if (_cyberpsychosis.TrySetStage(target, level))
                        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{admin.Name} set cyberpsychosis stage {level} on {ToPrettyString(target)}");
                },
            });
        }

        if (!HasComp<CyberpsychosisComponent>(target))
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString(CyberpsychosisAdminVerbCure),
            Category = VerbCategory.Admin,
            Impact = LogImpact.Medium,
            Act = () =>
            {
                _cyberpsychosis.Cure(target);
                _adminLog.Add(LogType.Action, LogImpact.Medium, $"{admin.Name} cured the cyberpsychosis of {ToPrettyString(target)}");
            },
        });
    }
}
