using Content.Shared._Aquila.Actions;
using Content.Shared.Actions.Components;

namespace Content.Shared.Actions;

public abstract partial class SharedActionsSystem
{
    private bool IsActionProviderBlocked(Entity<ActionComponent> action, EntityUid user)
    {
        if (action.Comp.Container is not { } provider || provider == user)
            return false;

        var ev = new ActionProviderAttemptEvent(user);
        RaiseLocalEvent(provider, ref ev);
        return ev.Cancelled;
    }
}
