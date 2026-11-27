using Content.Goobstation.Shared._Aquila.Illusion.Components;
using Content.Goobstation.Shared.Illusion;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Illusion;

public sealed partial class IllusionSystem
{
    private EntityUid? SpawnIllusion(EntityUid user, float lifetime, float healthMultiplier,
        IReadOnlyList<EntityUid> targets, ComponentRegistry components)
    {
        EntityUid? clone = null;
        try
        {
            return SpawnIllusionCore(user, lifetime, healthMultiplier, targets, components, out clone);
        }
        catch
        {
            if (clone is { } spawned && !TerminatingOrDeleted(spawned))
                QueueDel(spawned);

            throw;
        }
    }

    private void InitializeSpawnBudgets()
    {
        SubscribeLocalEvent<IllusionSpawnBudgetComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<BudgetedIllusionComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private bool CanSpawnIndependentIllusion(EntityUid user)
    {
        return !HasComp<BudgetedIllusionComponent>(user);
    }

    private bool ReserveIllusionSlot(ref SpawnIllusionEvent args, out IllusionSpawnBudgetComponent? budget)
    {
        budget = null;
        if (args.BudgetOwner is not { } owner || args.ActiveLimit is not { } limit)
            return true;

        if (TerminatingOrDeleted(owner) || EntityManager.IsQueuedForDeletion(owner) || limit <= 0)
            return false;

        budget = EnsureComp<IllusionSpawnBudgetComponent>(owner);
        budget.Illusions.RemoveAll(uid => TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid));
        if (budget.Illusions.Count + budget.Reservations >= limit)
        {
            budget = null;
            return false;
        }

        budget.Reservations++;
        return true;
    }

    private void RegisterBudgetedIllusion(EntityUid uid, EntityUid owner, IllusionSpawnBudgetComponent budget)
    {
        if (TerminatingOrDeleted(owner) || budget.Deleted)
        {
            QueueDel(uid);
            return;
        }

        EnsureComp<BudgetedIllusionComponent>(uid).BudgetOwner = owner;
        budget.Illusions.Add(uid);
    }

    private void OnComponentShutdown(Entity<BudgetedIllusionComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.BudgetOwner is { } owner && TryComp<IllusionSpawnBudgetComponent>(owner, out var budget))
            budget.Illusions.Remove(ent.Owner);
    }

    private void OnComponentShutdown(Entity<IllusionSpawnBudgetComponent> ent, ref ComponentShutdown args)
    {
        foreach (var illusion in ent.Comp.Illusions)
        {
            if (!TerminatingOrDeleted(illusion))
                QueueDel(illusion);
        }
    }
}
