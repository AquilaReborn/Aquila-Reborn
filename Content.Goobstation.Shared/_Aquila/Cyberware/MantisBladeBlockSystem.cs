using Content.Goobstation.Shared.MantisBlades;
using Content.Shared._Aquila.Cyberware;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Popups;

namespace Content.Goobstation.Shared._Aquila.Cyberware;

/// <summary>
/// Не даёт выдвигать богомольи клинки, пока они отключены ЭМИ или заглушены блокиратором.
/// </summary>
public sealed partial class MantisBladeBlockSystem : EntitySystem
{
    private static readonly LocId BlockedMessage = "cyberware-action-blocked";

    [Dependency] private CyberwareBlockSystem _block = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionComponent, ActionAttemptEvent>(OnActionAttempt);
    }

    private void OnActionAttempt(Entity<ActionComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled
            || !TryComp<MantisBladeUserComponent>(args.User, out var blades)
            || blades.ActionEntity != ent.Owner
            || blades.IsExtended
            || !_block.IsBlocked(args.User))
            return;

        args.Cancelled = true;
        _popup.PopupClient(Loc.GetString(BlockedMessage), args.User, args.User);
    }
}
