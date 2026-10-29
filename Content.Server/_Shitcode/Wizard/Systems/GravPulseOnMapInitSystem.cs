// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Goobstation.Wizard.Components;
using Content.Server.Singularity.EntitySystems;

namespace Content.Server._Goobstation.Wizard.Systems;

public sealed partial class GravPulseOnMapInitSystem : EntitySystem // Aquila Change
{
    [Dependency] private readonly GravityWellSystem _gravityWell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GravPulseOnMapInitComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<GravPulseOnMapInitComponent> ent, ref MapInitEvent args)
    {
        if (TryDeferPulse(ent)) // Aquila Change
            return; // Aquila Change

        Pulse(ent); // Aquila Change
    }

    private void Pulse(Entity<GravPulseOnMapInitComponent> ent) // Aquila Change
    {
        var (uid, comp) = ent;

        _gravityWell.GravPulse(uid,
            comp.MaxRange,
            comp.MinRange,
            comp.BaseRadialAcceleration,
            comp.BaseTangentialAcceleration);
    }
}
