using Content.Goobstation.Shared._Aquila.Changeling;

namespace Content.Goobstation.Shared.Changeling.Systems;

public abstract partial class SharedChanglingActionSystem
{
    private static readonly LocId TrueFormPopup = "changeling-action-fail-trueform";
    private static readonly LocId HostControlPopup = "changeling-action-fail-host-control";

    private bool IsActionBlockedByForm(EntityUid user)
    {
        if (HasComp<ChangelingTrueFormComponent>(user))
        {
            DoPopup(user, TrueFormPopup);
            return true;
        }

        if (HasComp<ChangelingHostControlledComponent>(user))
        {
            DoPopup(user, HostControlPopup);
            return true;
        }

        return false;
    }
}
