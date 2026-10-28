using Content.Goobstation.Shared.Power.PTL;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Aquila.CCVars;
using Content.Shared.Cargo.Components;
using Content.Shared.Interaction;
using Robust.Shared.Configuration;

namespace Content.Goobstation.Server.Power.PTL;

public sealed partial class PTLSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly StationSystem _station = default!;

    private bool TryDepositToStationBudget(Entity<PTLComponent> ent, AfterInteractUsingEvent args)
    {
        if (!_cfg.GetCVar(AquilaCVars.Economy))
            return false;

        args.Handled = true;
        if (_station.GetOwningStation(ent) is not { } station
            || !TryComp<StationBankAccountComponent>(station, out var bank))
        {
            _popup.PopupEntity(Loc.GetString("ptl-interact-no-budget"), ent);
            return true;
        }

        _cargo.UpdateBankAccount((station, bank), (int) ent.Comp.SpesosHeld, ent.Comp.Account);
        ent.Comp.SpesosHeld = 0;
        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("ptl-interact-budget"), ent);
        _aud.PlayPvs(_soundKaching, args.User);
        return true;
    }
}
