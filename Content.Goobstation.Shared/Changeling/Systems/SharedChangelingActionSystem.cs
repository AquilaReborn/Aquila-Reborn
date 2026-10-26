using Content.Goobstation.Shared._Aquila.Changeling; // Aquila Change
using Content.Goobstation.Shared.Changeling.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Atmos.Components;
using Content.Shared.Popups;

namespace Content.Goobstation.Shared.Changeling.Systems;

public abstract class SharedChanglingActionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly LocId TrueFormPopup = "changeling-action-fail-trueform"; // Aquila Change
    private static readonly LocId HostControlPopup = "changeling-action-fail-host-control"; // Aquila Change

    private EntityQuery<ChangelingIdentityComponent> _lingQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingActionComponent, ActionAttemptEvent>(OnActionAttempt);

        _lingQuery = GetEntityQuery<ChangelingIdentityComponent>();
    }

    private void OnActionAttempt(Entity<ChangelingActionComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_lingQuery.TryComp(args.User, out var ling))
        {
            DoPopup(args.User, ent.Comp.NotChangelingPopup);
            args.Cancelled = true;

            return;
        }

        // Aquila Change start
        if (HasComp<ChangelingTrueFormComponent>(args.User))
        {
            DoPopup(args.User, TrueFormPopup);
            args.Cancelled = true;

            return;
        }

        if (HasComp<ChangelingHostControlledComponent>(args.User))
        {
            DoPopup(args.User, HostControlPopup);
            args.Cancelled = true;

            return;
        }
        // Aquila Change end

        if (ent.Comp.RequireAbsorbed > ling.TotalAbsorbedEntities)
        {
            var delta = ent.Comp.RequireAbsorbed - ling.TotalAbsorbedEntities;
            var popup = Loc.GetString("changeling-action-fail-absorbed", ("number", delta));

            DoPopup(args.User, popup);
            args.Cancelled = true;

            return;
        }

        if (!ent.Comp.UseOnFire && OnFire(args.User))
        {
            DoPopup(args.User, ent.Comp.OnFirePopup, PopupType.LargeCaution);
            args.Cancelled = true;

            return;
        }

        if (!ent.Comp.UseInLastResort && ling.IsInLastResort
            || !ent.Comp.UseInLesserForm && ling.IsInLesserForm)
        {
            DoPopup(args.User, ent.Comp.LesserFormPopup);
            args.Cancelled = true;

            return;
        }
    }

    #region Helper Methods
    private void DoPopup(EntityUid user, LocId popup, PopupType popupType = PopupType.Small)
    {
        _popup.PopupClient(Loc.GetString(popup), user, user, popupType);
    }

    private void DoPopup(EntityUid user, string popup, PopupType popupType = PopupType.Small)
    {
        _popup.PopupClient(popup, user, user, popupType);
    }

    private bool OnFire(EntityUid user)
    {
        return HasComp<OnFireComponent>(user);
    }

    #endregion
}
