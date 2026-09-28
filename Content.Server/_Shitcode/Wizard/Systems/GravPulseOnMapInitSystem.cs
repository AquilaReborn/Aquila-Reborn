// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Goobstation.Wizard.Components;
using Content.Server.Singularity.EntitySystems;
using Robust.Shared.Map; // Aquila Change

namespace Content.Server._Goobstation.Wizard.Systems;

public sealed class GravPulseOnMapInitSystem : EntitySystem
{
    [Dependency] private readonly GravityWellSystem _gravityWell = default!;

    private readonly HashSet<EntityUid> _pending = new(); // Aquila Change

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GravPulseOnMapInitComponent, MapInitEvent>(OnMapInit);
    }

    // Aquila Change start
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pending.Count == 0)
            return;

        foreach (var uid in _pending)
        {
            if (TryComp<GravPulseOnMapInitComponent>(uid, out var comp)
                && Transform(uid).MapID != MapId.Nullspace)
                Pulse((uid, comp));
        }

        _pending.Clear();
    }
    // Aquila Change end

    private void OnMapInit(Entity<GravPulseOnMapInitComponent> ent, ref MapInitEvent args)
    {
        // Aquila Change start
        if (Transform(ent).MapID == MapId.Nullspace)
        {
            _pending.Add(ent);
            return;
        }

        Pulse(ent);
    }

    private void Pulse(Entity<GravPulseOnMapInitComponent> ent)
    {
        // Aquila Change end
        var (uid, comp) = ent;

        _gravityWell.GravPulse(uid,
            comp.MaxRange,
            comp.MinRange,
            comp.BaseRadialAcceleration,
            comp.BaseTangentialAcceleration);
    }
}
