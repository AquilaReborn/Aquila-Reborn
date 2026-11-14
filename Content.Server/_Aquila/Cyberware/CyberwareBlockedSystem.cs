using Content.Goobstation.Shared.MantisBlades;
using Content.Goobstation.Shared.Sandevistan;
using Content.Shared._Aquila.Cyberware.Components;
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
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SandevistanSystem _sandevistan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberwareBlockedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CyberwareBlockedComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<CyberwareBlockedComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<SandevistanUserComponent>(ent, out var sandevistan) && sandevistan.Active)
            _sandevistan.Disable(ent, sandevistan);

        RetractMantisBlades(ent);

        _cyberLoad.QueueRecalculationWithoutStrain(ent);
        _popup.PopupEntity(Loc.GetString(BlockedMessage), ent, ent, PopupType.MediumCaution);
    }

    private void OnShutdown(Entity<CyberwareBlockedComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _cyberLoad.QueueRecalculationWithoutStrain(ent);
        _popup.PopupEntity(Loc.GetString(UnblockedMessage), ent, ent, PopupType.Medium);
    }

    private void RetractMantisBlades(EntityUid body)
    {
        if (!TryComp<MantisBladeUserComponent>(body, out var blades) || !blades.IsExtended)
            return;

        RaiseLocalEvent(body, new ToggleMantisBladesActionEvent());
    }
}
