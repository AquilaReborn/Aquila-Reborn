using Content.Server.Body.Components;
using Content.Server.Nutrition.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCUtilitySystem
{
    private readonly Queue<ReagentQuantity> _foodReagentQueue = new();
    private readonly HashSet<ReagentPrototype> _foodCheckedReagents = new();
    private EntityUid? _metabolizersOwner;
    private List<Entity<MetabolizerComponent, OrganComponent>>? _metabolizers;

    private bool IsFoodWorthEating(EntityUid food, EntityUid owner, bool avoidBadFood)
    {
        if (avoidBadFood && HasComp<BadFoodComponent>(food))
            return false;

        var foodUtility = TotalFoodUtilityConditioned(food, owner);
        if (avoidBadFood && foodUtility.IsToxic)
            return false;

        return foodUtility.TotalNutrition > 0.0f;
    }

    private bool IsDrinkWorthDrinking(EntityUid drink, EntityUid owner)
    {
        if (HasComp<BadDrinkComponent>(drink))
            return false;

        var drinkUtility = TotalFoodUtilityConditioned(drink, owner);
        if (drinkUtility.IsToxic)
            return false;

        // needs to have something that will satiate thirst, mice wont try to drink 100% pure mutagen.
        // We don't check if the solution is metabolizable cause all drinks should be currently.
        // If that changes then simply use the other overflow.
        return drinkUtility.TotalHydration > 1.0f;
    }

    private EntityUid? GetMetabolizingOrgan(EntityUid consumer, ProtoId<MetabolismGroupPrototype> metabolism)
    {
        if (_metabolizersOwner != consumer)
        {
            _metabolizersOwner = consumer;
            _metabolizers = null;

            if (HasComp<BodyComponent>(consumer))
                _body.TryGetBodyOrganEntityComps<MetabolizerComponent>(consumer, out _metabolizers);
        }

        if (_metabolizers == null)
            return null;

        foreach (var organ in _metabolizers)
        {
            if (organ.Comp1.MetabolismGroups == null)
                continue;

            foreach (var group in organ.Comp1.MetabolismGroups)
            {
                if (group.Id == metabolism)
                    return organ.Owner;
            }
        }

        return null;
    }

    private (Queue<ReagentQuantity> Queue, HashSet<ReagentPrototype> Checked) GetClearedFoodBuffers()
    {
        _foodReagentQueue.Clear();
        _foodCheckedReagents.Clear();
        return (_foodReagentQueue, _foodCheckedReagents);
    }

    private void ResolveMetabolizingOrgan(
        bool needed,
        EntityUid consumer,
        ProtoId<MetabolismGroupPrototype> metabolism,
        ref bool resolved,
        ref EntityUid? organ)
    {
        if (!needed || resolved)
            return;

        organ = GetMetabolizingOrgan(consumer, metabolism);
        resolved = true;
    }
}
