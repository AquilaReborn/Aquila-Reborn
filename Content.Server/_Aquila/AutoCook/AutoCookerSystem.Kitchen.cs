using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Aquila.AutoCook;
using Content.Shared.Chemistry.Components;

namespace Content.Server._Aquila.AutoCook;

public sealed partial class AutoCookerSystem
{
    private (Dictionary<string, int> Items, Dictionary<string, float> Reagents) GetKitchenInventory(Entity<AutoCookerComponent> ent)
    {
        var items = new Dictionary<string, int>();
        var reagents = new Dictionary<string, float>();

        foreach (var item in GetStoredItems(ent))
        {
            if (MetaData(item).EntityPrototype is not { } proto)
                continue;

            items[proto.ID] = items.GetValueOrDefault(proto.ID) + 1;
        }

        foreach (var source in GetLiquidSources(ent))
        {
            foreach (var quantity in source.Comp.Solution.Contents)
            {
                var id = quantity.Reagent.Prototype;
                reagents[id] = reagents.GetValueOrDefault(id) + quantity.Quantity.Float();
            }
        }

        return (items, reagents);
    }

    private KitchenPlan CreateKitchenPlan(Entity<AutoCookerComponent> ent)
    {
        var (items, reagents) = GetKitchenInventory(ent);
        return new KitchenPlan(items, reagents);
    }

    private IReadOnlyList<EntityUid> GetStoredItems(Entity<AutoCookerComponent> ent)
    {
        return _container.TryGetContainer(ent, ent.Comp.StorageContainer, out var container)
            ? container.ContainedEntities
            : [];
    }

    private List<Entity<SolutionComponent>> GetLiquidSources(Entity<AutoCookerComponent> ent)
    {
        var sources = new List<Entity<SolutionComponent>>();

        foreach (var item in GetStoredItems(ent))
        {
            if (_solution.TryGetDrainableSolution(item, out var soln, out _))
                sources.Add(soln.Value);
        }

        if (_solution.TryGetSolution(ent.Owner, ent.Comp.PantrySolution, out var pantry, out _))
            sources.Add(pantry.Value);

        return sources;
    }

    private bool DrainLiquids(Entity<AutoCookerComponent> ent, Dictionary<string, float> needs)
    {
        var sources = GetLiquidSources(ent);

        foreach (var (id, amount) in needs)
        {
            var remaining = amount;
            foreach (var source in sources)
            {
                if (remaining <= Epsilon)
                    break;

                remaining -= _solution.RemoveReagent(source, id, FixedPoint2.New(remaining)).Float();
            }

            if (remaining > 0.05f)
                return false;
        }

        return true;
    }

    private bool ConsumeKitchenIngredients(Entity<AutoCookerComponent> ent, Dictionary<string, int> solids, Dictionary<string, float> reagents)
    {
        var stored = GetStoredItems(ent);
        var toDelete = new List<EntityUid>();

        foreach (var (id, count) in solids)
        {
            var matching = stored
                .Where(item => !toDelete.Contains(item) && MetaData(item).EntityPrototype?.ID == id)
                .Take(count)
                .ToList();

            if (matching.Count < count)
                return false;

            toDelete.AddRange(matching);
        }

        if (reagents.Count > 0 && !DrainLiquids(ent, reagents))
            return false;

        foreach (var item in toDelete)
        {
            QueueDel(item);
        }

        return true;
    }

    private void RefundKitchenJob(Entity<AutoCookerComponent> ent, AutoCookJob job)
    {
        if (ent.Comp.Kind != AutoCookKind.Kitchen || job.StepIndex >= job.Steps.Count)
            return;

        if (_container.TryGetContainer(ent, ent.Comp.StorageContainer, out var container))
        {
            var coords = Transform(ent).Coordinates;
            foreach (var (id, count) in job.ConsumedSolids)
            {
                for (var i = 0; i < count; i++)
                {
                    _container.Insert(Spawn(id, coords), container);
                }
            }
        }

        if (job.ConsumedReagents.Count > 0
            && _solution.TryGetSolution(ent.Owner, ent.Comp.PantrySolution, out var pantry, out _))
        {
            foreach (var (id, amount) in job.ConsumedReagents)
            {
                _solution.TryAddReagent(pantry.Value, id, FixedPoint2.New(amount));
            }
        }
    }

    private void DropKitchenJob(Entity<AutoCookerComponent> ent, AutoCookJob job)
    {
        var xform = Transform(ent);
        if (xform.MapUid is not { } map || TerminatingOrDeleted(map))
            return;

        var coords = xform.Coordinates;
        foreach (var (id, count) in job.ConsumedSolids)
        {
            for (var i = 0; i < count; i++)
            {
                Spawn(id, coords);
            }
        }
    }

    private AutoCookJob? TryStartKitchen(Entity<AutoCookerComponent> ent, string recipeId)
    {
        if (recipeId.StartsWith(AutoCookIds.Meal))
            return TryStartMeal(ent, recipeId[AutoCookIds.Meal.Length..]);

        if (recipeId.StartsWith(AutoCookIds.Mix))
            return TryStartMix(ent, recipeId[AutoCookIds.Mix.Length..]);

        if (recipeId.StartsWith(AutoCookIds.Make))
            return TryStartMake(ent, recipeId[AutoCookIds.Make.Length..]);

        return null;
    }

    private AutoCookJob? TryStartMeal(Entity<AutoCookerComponent> ent, string id)
    {
        var recipe = _meals.FirstOrDefault(meal => meal.ID == id);
        if (recipe == null)
            return null;

        var plan = CreateKitchenPlan(ent);
        if (!TryPlanMeal(ent.Comp, recipe, plan))
            return null;

        plan.Steps.Insert(0, new AutoCookStep
        {
            Kind = AutoCookStepKind.Prepare,
            Text = Loc.GetString("autocook-step-prepare"),
            Duration = ent.Comp.PrepareSeconds,
        });

        return CommitKitchenJob(ent, plan, MealName(recipe), recipe.Result, 1);
    }

    private AutoCookJob? TryStartMix(Entity<AutoCookerComponent> ent, string reactionId)
    {
        foreach (var (reaction, entity, number) in _mixList.Where(mix => mix.Reaction.ID == reactionId))
        {
            var plan = CreateKitchenPlan(ent);
            if (!TryApplyMix(ent.Comp, reaction, entity, number, plan))
                return null;

            return CommitKitchenJob(ent, plan, EntityName(entity), entity, number);
        }

        return null;
    }

    private AutoCookJob? TryStartMake(Entity<AutoCookerComponent> ent, string target)
    {
        if (!_transformSources.ContainsKey(target))
            return null;

        var plan = CreateKitchenPlan(ent);
        if (!TryProduceSolid(ent.Comp, target, null, plan, 0) || plan.Steps.Count == 0)
            return null;

        return CommitKitchenJob(ent, plan, EntityName(target), target, 1);
    }

    private AutoCookJob? CommitKitchenJob(Entity<AutoCookerComponent> ent, KitchenPlan plan, string name, string result, int count)
    {
        if (!ConsumeKitchenIngredients(ent, plan.ConsumedSolids, plan.ConsumedReagents))
            return null;

        return new AutoCookJob
        {
            RecipeName = name,
            Steps = plan.Steps,
            ResultEntity = result,
            ResultCount = count,
            ConsumedSolids = plan.ConsumedSolids,
            ConsumedReagents = plan.ConsumedReagents,
        };
    }
}
