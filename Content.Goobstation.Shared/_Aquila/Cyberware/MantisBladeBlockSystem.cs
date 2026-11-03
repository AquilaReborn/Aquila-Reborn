using Content.Goobstation.Shared.MantisBlades;
using Content.Shared._Aquila.Actions;
using Content.Shared._Aquila.Cyberware;

namespace Content.Goobstation.Shared._Aquila.Cyberware;

/// <summary>
/// Не даёт выдвигать богомольи клинки, пока они отключены ЭМИ или заглушены блокиратором.
/// </summary>
public sealed partial class MantisBladeBlockSystem : EntitySystem
{
    [Dependency] private CyberwareBlockSystem _block = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MantisBladeArmComponent, ActionProviderAttemptEvent>(OnActionAttempt);
    }

    private void OnActionAttempt(Entity<MantisBladeArmComponent> ent, ref ActionProviderAttemptEvent args)
    {
        _block.TryBlockAction(ent, ref args);
    }
}
