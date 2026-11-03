using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared._Aquila.Cyberware.Components;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server._Aquila.Cyberware;

/// <summary>
/// Админское действие: вылечить киберпсихоз.
/// </summary>
public sealed class CyberpsychosisAdminSystem : EntitySystem
{
    private static readonly LocId CyberpsychosisAdminVerbCure = "cyberpsychosis-admin-verb-cure";

    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly CyberpsychosisSystem _cyberpsychosis = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberpsychosisComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<CyberpsychosisComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor)
            || !_admin.HasAdminFlag(actor.PlayerSession, AdminFlags.Fun))
            return;

        var admin = actor.PlayerSession;
        var target = ent.Owner;

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
