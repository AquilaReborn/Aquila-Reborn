using Content.Goobstation.Shared.MantisBlades;
using Content.Goobstation.Shared.Sandevistan;
using Content.Shared._Aquila.Cyberware.Components;
using Content.Shared.Actions;
using Content.Shared.Popups;

namespace Content.Server._Aquila.Cyberware;

/// <summary>
/// Что происходит с телом, когда на него надевают и снимают блокиратор имплантов.
/// </summary>
public sealed class CyberwareBlockedSystem : EntitySystem
{
    private static readonly LocId BlockedMessage = "cyberware-blocked";
    private static readonly LocId UnblockedMessage = "cyberware-unblocked";

    [Dependency] private readonly CyberLoadSystem _cyberLoad = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SandevistanSystem _sandevistan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberwareBlockedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CyberwareBlockedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CyberwareBlockedComponent, ActionAddedEvent>(OnMantisBladesActionAdded);
    }

    private void OnStartup(Entity<CyberwareBlockedComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<SandevistanUserComponent>(ent, out var sandevistan) && sandevistan.Active)
            _sandevistan.Disable(ent, sandevistan);

        if (TryComp<MantisBladeUserComponent>(ent, out var blades))
            _actions.SetEnabled(blades.ActionEntity, false);

        RetractMantisBlades(ent);

        _cyberLoad.QueueRecalculationWithoutStrain(ent);
        _popup.PopupEntity(Loc.GetString(BlockedMessage), ent, ent, PopupType.MediumCaution);
    }

    private void OnShutdown(Entity<CyberwareBlockedComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (TryComp<MantisBladeUserComponent>(ent, out var blades))
            _actions.SetEnabled(blades.ActionEntity, true);

        _cyberLoad.QueueRecalculationWithoutStrain(ent);
        _popup.PopupEntity(Loc.GetString(UnblockedMessage), ent, ent, PopupType.Medium);
    }

    private void OnMantisBladesActionAdded(Entity<CyberwareBlockedComponent> ent, ref ActionAddedEvent args)
    {
        if (!TryComp<MantisBladeUserComponent>(ent, out var blades)
            || Prototype(args.Action)?.ID != blades.Action.Id)
            return;

        _actions.SetEnabled(args.Action, false);
    }

    private void RetractMantisBlades(EntityUid body)
    {
        if (!TryComp<MantisBladeUserComponent>(body, out var blades) || !blades.IsExtended)
            return;

        RaiseLocalEvent(body, new ToggleMantisBladesActionEvent());
    }
}
