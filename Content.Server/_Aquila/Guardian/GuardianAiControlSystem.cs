using Content.Server.Guardian;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Robust.Shared.Containers;

namespace Content.Goobstation.Server.Aquila.Guardian;

public sealed class GuardianAiControlSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GuardianHostComponent, EntInsertedIntoContainerMessage>(OnGuardianInserted);
        SubscribeLocalEvent<GuardianHostComponent, EntRemovedFromContainerMessage>(OnGuardianRemoved);
    }

    private void OnGuardianInserted(EntityUid uid, GuardianHostComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != component.GuardianContainer.ID)
            return;

        var guardian = args.Entity;

        if (TryComp<HTNComponent>(guardian, out var htn))
            htn.Enabled = false;

        ClearCombatTargets(guardian);
    }

    private void OnGuardianRemoved(EntityUid uid, GuardianHostComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.GuardianContainer.ID)
            return;

        if (TryComp<HTNComponent>(args.Entity, out var htn))
            htn.Enabled = true;
    }

    private void ClearCombatTargets(EntityUid guardian)
    {
        if (TryComp<NPCMeleeCombatComponent>(guardian, out var melee))
        {
            melee.Target = default;
            melee.Status = CombatStatus.Unspecified;
        }

        if (TryComp<NPCRangedCombatComponent>(guardian, out var ranged))
        {
            ranged.Target = default;
            ranged.Status = CombatStatus.Unspecified;
            ranged.ShootAccumulator = 0f;
        }
    }
}
