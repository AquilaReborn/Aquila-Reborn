using Content.Shared._Aquila.CCVars;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    private bool IsWithdrawDisabled(Entity<CargoOrderConsoleComponent> console, EntityUid actor, ProtoId<CargoAccountPrototype>? account)
    {
        if (account != null || !_cfg.GetCVar(AquilaCVars.Economy))
            return false;

        ConsolePopup(actor, Loc.GetString("cargo-console-withdraw-disabled"));
        PlayDenySound(console, console.Comp);
        return true;
    }
}
