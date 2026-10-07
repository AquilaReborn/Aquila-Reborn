using Content.Goobstation.Shared.Vehicles;
using Content.Server.NPC.HTN;
using Content.Shared.Buckle.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Goobstation.Server.Aquila.Vehicles;

public sealed class VehicleAiControlSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HTNComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<HTNComponent, UnstrappedEvent>(OnUnstrapped);
    }

    private void OnStrapped(EntityUid uid, HTNComponent component, ref StrappedEvent args)
    {
        component.Enabled = false;
    }

    private void OnUnstrapped(EntityUid uid, HTNComponent component, ref UnstrappedEvent args)
    {
        component.Enabled = true;
    }
}
