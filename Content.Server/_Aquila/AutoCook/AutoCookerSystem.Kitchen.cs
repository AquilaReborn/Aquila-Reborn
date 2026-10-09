using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Aquila.AutoCook;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Kitchen;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.AutoCook;

/// <summary>
/// Кухня: блюда по рецептам микроволновки из ингредиентов в хранилище и жидкостей в кладовой.
/// Недостающие ингредиенты машина пытается сделать сама: приготовить, замесить, нарезать или обработать.
/// </summary>
public sealed partial class AutoCookerSystem
{
    private const int MaxMealDepth = 4;

    /// <summary>
    /// Черновик плана готовки. Ветка создаётся на каждую попытку и принимается, только если попытка удалась.
    /// </summary>
    private sealed class KitchenPlan(
        Dictionary<EntProtoId, int> items,
        Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> reagents)
    {
        public Dictionary<EntProtoId, int> Items = items;
        public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Reagents = reagents;
        public readonly List<AutoCookStep> Steps = new();
        public readonly Dictionary<EntProtoId, int> ConsumedSolids = new();
        public readonly Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> ConsumedReagents = new();

        public KitchenPlan Branch()
        {
            return new KitchenPlan(new Dictionary<EntProtoId, int>(Items), new Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>(Reagents));
        }

        public void Adopt(KitchenPlan branch)
        {
            Items = branch.Items;
            Reagents = branch.Reagents;
            Steps.AddRange(branch.Steps);

            foreach (var (id, count) in branch.ConsumedSolids)
            {
                ConsumedSolids[id] = ConsumedSolids.GetValueOrDefault(id) + count;
            }

            foreach (var (id, amount) in branch.ConsumedReagents)
            {
                ConsumedReagents[id] = ConsumedReagents.GetValueOrDefault(id) + amount;
            }
        }

        public bool Has(ProtoId<ReagentPrototype> reagent, FixedPoint2 amount)
        {
            return Reagents.GetValueOrDefault(reagent) >= amount;
        }

        public bool TryTakeReagent(ProtoId<ReagentPrototype> reagent, FixedPoint2 amount)
        {
            if (!Has(reagent, amount))
                return false;

            Reagents[reagent] -= amount;
            ConsumedReagents[reagent] = ConsumedReagents.GetValueOrDefault(reagent) + amount;
            return true;
        }

        public bool TryTakeSolid(EntProtoId solid)
        {
            if (Items.GetValueOrDefault(solid) <= 0)
                return false;

            Items[solid]--;
            ConsumedSolids[solid] = ConsumedSolids.GetValueOrDefault(solid) + 1;
            return true;
        }

        public void AddSolid(EntProtoId solid, int count)
        {
            Items[solid] = Items.GetValueOrDefault(solid) + count;
        }
    }

    private AutoCookJob? TryCreateKitchenJob(Entity<AutoCookerComponent> ent, AutoCookRecipeId recipe)
    {
        var plan = CreateKitchenPlan(ent);
        EntProtoId result;
        var count = 1;

        switch (recipe.Kind)
        {
            case AutoCookRecipeKind.Meal:
                if (!TryGetMeal(ent.Comp, recipe.Id, out var meal) || !TryPlanMeal(ent.Comp, meal, plan, 0))
                    return null;

                plan.Steps.Insert(0, new AutoCookStep(new AutoCookStepData(AutoCookStepKind.PrepareIngredients, string.Empty, 0f, ent.Comp.PrepareDuration)));
                result = meal.Result;
                break;
            case AutoCookRecipeKind.Mix:
                if (!TryGetMix(recipe.Id, out var mix) || !TryApplyMix(ent.Comp, mix, plan))
                    return null;

                result = mix.Entity;
                count = mix.Number;
                break;
            case AutoCookRecipeKind.Make:
                if (!_transformSources.ContainsKey(recipe.Id) || !TryProduceSolid(ent.Comp, recipe.Id, null, plan, 0))
                    return null;

                result = recipe.Id;
                break;
            default:
                return null;
        }

        if (plan.Steps.Count == 0 || !TryConsumeKitchenIngredients(ent, plan))
            return null;

        return new AutoCookJob(recipe, result, plan.Steps)
        {
            ResultCount = count,
            ConsumedReagents = plan.ConsumedReagents,
        };
    }

    private bool TryGetMeal(AutoCookerComponent comp, string id, [NotNullWhen(true)] out FoodRecipePrototype? meal)
    {
        meal = _meals.FirstOrDefault(recipe => recipe.ID == id);
        return meal != null && HasRealIngredients(comp, meal);
    }

    private bool TryGetMix(string reactionId, [NotNullWhen(true)] out KitchenMix? mix)
    {
        mix = _mixes.FirstOrDefault(candidate => candidate.Reaction.ID == reactionId);
        return mix != null;
    }

    /// <summary>
    /// Рецепт, где кроме посуды ничего не нужно, машина не готовит, иначе блюдо появлялось бы из воздуха.
    /// </summary>
    private static bool HasRealIngredients(AutoCookerComponent comp, FoodRecipePrototype meal)
    {
        return meal.IngredientsReagents.Count > 0
               || meal.IngredientsSolids.Keys.Any(solid => !IsFreeIngredient(comp, solid));
    }

    private static bool IsFreeIngredient(AutoCookerComponent comp, string id)
    {
        return comp.FreeIngredients.Contains(id);
    }

    private bool TryPlanMeal(AutoCookerComponent comp, FoodRecipePrototype recipe, KitchenPlan plan, int depth)
    {
        if (depth > MaxMealDepth)
            return false;

        var work = plan.Branch();

        foreach (var (solid, count) in recipe.IngredientsSolids)
        {
            if (IsFreeIngredient(comp, solid))
                continue;

            for (var i = 0; i < count.Int(); i++)
            {
                if (!work.TryTakeSolid(solid) && !TryProduceSolid(comp, solid, recipe, work, depth))
                    return false;
            }
        }

        foreach (var (reagent, quantity) in recipe.IngredientsReagents)
        {
            if (!work.TryTakeReagent(reagent, quantity))
                return false;
        }

        var cookTime = TimeSpan.FromSeconds(recipe.CookTime) * comp.CookTimeMultiplier;
        var duration = TimeSpan.FromTicks(Math.Max(comp.PrepareDuration.Ticks, Math.Min(cookTime.Ticks, comp.MaxCookDuration.Ticks)));
        work.Steps.Add(new AutoCookStep(new AutoCookStepData(AutoCookStepKind.Cook, recipe.Result, 0f, duration)));

        plan.Adopt(work);
        return true;
    }

    /// <summary>
    /// Делает одну штуку <paramref name="solid"/> и сразу расходует её.
    /// <paramref name="exclude"/> не даёт рецепту приготовить сам себя.
    /// </summary>
    private bool TryProduceSolid(AutoCookerComponent comp, string solid, FoodRecipePrototype? exclude, KitchenPlan plan, int depth)
    {
        if (depth > MaxMealDepth)
            return false;

        return TryProduceByMeal(comp, solid, exclude, plan, depth)
               || TryProduceByMix(comp, solid, plan)
               || TryProduceBySlice(comp, solid, exclude, plan, depth)
               || TryProduceByTransform(comp, solid, exclude, plan, depth);
    }

    private bool TryProduceByMeal(AutoCookerComponent comp, string solid, FoodRecipePrototype? exclude, KitchenPlan plan, int depth)
    {
        foreach (var meal in _meals)
        {
            if (meal.Result != solid || meal == exclude)
                continue;

            var branch = plan.Branch();
            if (!TryPlanMeal(comp, meal, branch, depth + 1))
                continue;

            plan.Adopt(branch);
            return true;
        }

        return false;
    }

    private bool TryProduceByMix(AutoCookerComponent comp, string solid, KitchenPlan plan)
    {
        return _mixesByEntity.TryGetValue(solid, out var mixes)
               && mixes.Any(mix => TryApplyMix(comp, mix, plan));
    }

    private static bool TryApplyMix(AutoCookerComponent comp, KitchenMix mix, KitchenPlan plan)
    {
        if (!mix.Reaction.Reactants.All(pair => plan.Has(pair.Key, pair.Value.Amount)))
            return false;

        foreach (var (reactant, data) in mix.Reaction.Reactants)
        {
            if (!data.Catalyst)
                plan.TryTakeReagent(reactant, data.Amount);
        }

        plan.Steps.Add(new AutoCookStep(new AutoCookStepData(AutoCookStepKind.Mix, mix.Entity, 0f, comp.PrepareDuration)));

        if (mix.Number > 1)
            plan.AddSolid(mix.Entity, mix.Number - 1);

        return true;
    }

    private bool TryProduceBySlice(AutoCookerComponent comp, string solid, FoodRecipePrototype? exclude, KitchenPlan plan, int depth)
    {
        if (!_sliceSources.TryGetValue(solid, out var wholes))
            return false;

        foreach (var (whole, count) in wholes)
        {
            if (whole == solid)
                continue;

            var branch = plan.Branch();
            if (!TryTakeOrProduce(comp, whole, exclude, branch, depth + 1))
                continue;

            branch.Steps.Add(new AutoCookStep(new AutoCookStepData(AutoCookStepKind.Slice, solid, 0f, comp.SliceDuration)));

            if (count > 1)
                branch.AddSolid(solid, count - 1);

            plan.Adopt(branch);
            return true;
        }

        return false;
    }

    private bool TryProduceByTransform(AutoCookerComponent comp, string solid, FoodRecipePrototype? exclude, KitchenPlan plan, int depth)
    {
        if (!_transformSources.TryGetValue(solid, out var sources))
            return false;

        foreach (var source in sources)
        {
            var branch = plan.Branch();
            if (!TryTakeOrProduce(comp, source, exclude, branch, depth + 1))
                continue;

            branch.Steps.Add(new AutoCookStep(new AutoCookStepData(AutoCookStepKind.Process, solid, 0f, comp.PrepareDuration)));
            plan.Adopt(branch);
            return true;
        }

        return false;
    }

    private bool TryTakeOrProduce(AutoCookerComponent comp, EntProtoId solid, FoodRecipePrototype? exclude, KitchenPlan plan, int depth)
    {
        return plan.TryTakeSolid(solid) || TryProduceSolid(comp, solid, exclude, plan, depth);
    }

    private KitchenPlan CreateKitchenPlan(Entity<AutoCookerComponent> ent)
    {
        var items = new Dictionary<EntProtoId, int>();
        var reagents = new Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>();

        foreach (var item in GetStoredItems(ent))
        {
            if (GetSolid(item) is { } solid)
                items[solid.Id] = items.GetValueOrDefault(solid.Id) + solid.Count;
        }

        foreach (var source in GetLiquidSources(ent))
        {
            foreach (var quantity in source.Comp.Solution.Contents)
            {
                ProtoId<ReagentPrototype> id = quantity.Reagent.Prototype;
                reagents[id] = reagents.GetValueOrDefault(id) + quantity.Quantity;
            }
        }

        return new KitchenPlan(items, reagents);
    }

    private (EntProtoId Id, int Count)? GetSolid(EntityUid item)
    {
        if (TryComp<StackComponent>(item, out var stack))
        {
            return _proto.TryIndex(stack.StackTypeId, out var stackType)
                ? (stackType.Spawn, stack.Count)
                : null;
        }

        return MetaData(item).EntityPrototype is { } proto ? (proto.ID, 1) : null;
    }

    private IReadOnlyList<EntityUid> GetStoredItems(Entity<AutoCookerComponent> ent)
    {
        return _container.TryGetContainer(ent, ent.Comp.StorageContainer, out var container)
            ? container.ContainedEntities
            : [];
    }

    /// <summary>
    /// Жидкости берутся из ёмкостей в хранилище и из встроенной кладовой.
    /// </summary>
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

    private bool TryConsumeKitchenIngredients(Entity<AutoCookerComponent> ent, KitchenPlan plan)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ProcessingContainer, out var processing))
            return false;

        var stored = GetStoredItems(ent);
        var picked = new Dictionary<EntityUid, int>();

        foreach (var (id, count) in plan.ConsumedSolids)
        {
            var remaining = count;
            foreach (var item in stored)
            {
                if (remaining <= 0)
                    break;

                if (picked.ContainsKey(item) || GetSolid(item) is not { } solid || solid.Id != id)
                    continue;

                var take = Math.Min(solid.Count, remaining);
                picked[item] = take;
                remaining -= take;
            }

            if (remaining > 0)
                return false;
        }

        if (!TryDrainLiquids(ent, plan.ConsumedReagents))
            return false;

        var coords = Transform(ent).Coordinates;
        foreach (var (item, count) in picked)
        {
            var moved = item;
            if (TryComp<StackComponent>(item, out var stack) && stack.Count > count)
                moved = _stack.Split((item, stack), count, coords) ?? item;

            _container.Insert(moved, processing);
        }

        return true;
    }

    private bool TryDrainLiquids(Entity<AutoCookerComponent> ent, Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> needs)
    {
        if (needs.Count == 0)
            return true;

        var sources = GetLiquidSources(ent);

        foreach (var (id, amount) in needs)
        {
            var remaining = amount;
            foreach (var source in sources)
            {
                if (remaining <= FixedPoint2.Zero)
                    break;

                remaining -= _solution.RemoveReagent(source, id, remaining);
            }

            if (remaining > FixedPoint2.Zero)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Отмена задания возвращает потраченные ингредиенты: предметы в хранилище, жидкости в кладовую.
    /// </summary>
    private void RefundKitchenJob(Entity<AutoCookerComponent> ent, AutoCookJob job)
    {
        if (job.Finished)
            return;

        if (_container.TryGetContainer(ent, ent.Comp.ProcessingContainer, out var processing))
        {
            foreach (var item in processing.ContainedEntities.ToArray())
            {
                if (!_storage.Insert(ent, item, out _, playSound: false))
                    _container.Remove(item, processing);
            }
        }

        if (job.ConsumedReagents.Count == 0
            || !_solution.TryGetSolution(ent.Owner, ent.Comp.PantrySolution, out var pantry, out _))
            return;

        foreach (var (id, amount) in job.ConsumedReagents)
        {
            _solution.TryAddReagent(pantry.Value, id, amount);
        }
    }

    private void ClearProcessing(Entity<AutoCookerComponent> ent)
    {
        if (_container.TryGetContainer(ent, ent.Comp.ProcessingContainer, out var processing))
            _container.CleanContainer(processing);
    }
}
