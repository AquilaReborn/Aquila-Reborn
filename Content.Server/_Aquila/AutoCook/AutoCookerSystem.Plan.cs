using System.Linq;
using Content.Shared._Aquila.AutoCook;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Kitchen;

namespace Content.Server._Aquila.AutoCook;

public sealed partial class AutoCookerSystem
{
    private const int MaxReagentDepth = 6;
    private const int MaxMealDepth = 4;
    private const float MaxCookSeconds = 30f;
    private const float Epsilon = 0.001f;
    private const float RoomTemperature = 293.15f;

    private sealed class KitchenPlan(Dictionary<string, int> items, Dictionary<string, float> reagents)
    {
        public Dictionary<string, int> Items = items;
        public Dictionary<string, float> Reagents = reagents;
        public readonly List<AutoCookStep> Steps = new();
        public readonly Dictionary<string, int> ConsumedSolids = new();
        public readonly Dictionary<string, float> ConsumedReagents = new();

        public KitchenPlan Branch()
        {
            return new KitchenPlan(new Dictionary<string, int>(Items), new Dictionary<string, float>(Reagents));
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
                ConsumeReagent(id, amount);
            }
        }

        public bool Has(string reagent, float amount)
        {
            return Reagents.GetValueOrDefault(reagent) + Epsilon >= amount;
        }

        public void ConsumeReagent(string reagent, float amount)
        {
            ConsumedReagents[reagent] = ConsumedReagents.GetValueOrDefault(reagent) + amount;
        }

        public void TakeSolid(string solid)
        {
            Items[solid]--;
            ConsumedSolids[solid] = ConsumedSolids.GetValueOrDefault(solid) + 1;
        }
    }

    private bool TryPlanReagent(AutoCookerComponent comp, string reagent, float amount, List<AutoCookStep> steps, int depth = 0)
    {
        if (depth > MaxReagentDepth || amount <= 0f)
            return false;

        if (IsBaseReagent(comp, reagent))
        {
            steps.Add(new AutoCookStep
            {
                Kind = AutoCookStepKind.Synthesize,
                Subject = reagent,
                Produce = { [reagent] = amount },
            });

            return true;
        }

        if (!_reactionsByProduct.TryGetValue(reagent, out var reactions))
            return false;

        foreach (var reaction in reactions)
        {
            var produced = reaction.Products[reagent].Float();
            if (produced <= 0f)
                continue;

            if (!CanMix(comp, reaction))
                continue;

            var batches = MathF.Ceiling(amount / produced);
            var attempt = new List<AutoCookStep>();

            if (!reaction.Reactants.All(pair => TryPlanReagent(comp, pair.Key, pair.Value.Amount.Float() * batches, attempt, depth + 1)))
                continue;

            AddConditionSteps(comp, reaction, attempt);

            var step = new AutoCookStep
            {
                Kind = AutoCookStepKind.React,
                Subject = reagent,
                Duration = comp.ReactionSeconds,
            };

            foreach (var (reactant, data) in reaction.Reactants.Where(pair => !pair.Value.Catalyst))
            {
                step.Consume[reactant] = data.Amount.Float() * batches;
            }

            foreach (var (product, quantity) in reaction.Products)
            {
                step.Produce[product] = quantity.Float() * batches;
            }

            attempt.Add(step);
            steps.AddRange(attempt);
            return true;
        }

        return false;
    }

    private List<AutoCookStep> FinalizeReagentSteps(AutoCookerComponent comp, List<AutoCookStep> raw)
    {
        var result = new List<AutoCookStep>();
        var synthesis = new Dictionary<string, AutoCookStep>();

        foreach (var step in raw)
        {
            if (step.Kind == AutoCookStepKind.Synthesize)
            {
                if (synthesis.TryGetValue(step.Subject, out var existing))
                {
                    existing.Produce[step.Subject] += step.Produce[step.Subject];
                    continue;
                }

                synthesis[step.Subject] = step;
            }

            result.Add(step);
        }

        foreach (var step in result)
        {
            if (step.Kind == AutoCookStepKind.Synthesize)
            {
                var amount = step.Produce[step.Subject];
                step.Duration = comp.SynthesisSeconds + amount * comp.SecondsPerUnit;
                step.Text = Loc.GetString("autocook-step-synthesize",
                    ("name", ReagentName(step.Subject)),
                    ("amount", FormatAmount(amount)));
            }
            else if (step.Kind == AutoCookStepKind.React)
            {
                step.Text = Loc.GetString("autocook-step-react", ("name", ReagentName(step.Subject)));
            }
        }

        return result;
    }

    private static bool CanMix(AutoCookerComponent comp, ReactionPrototype reaction)
    {
        return reaction.MixingCategories == null
               || reaction.MixingCategories.All(category => comp.AllowedMixing.Contains(category.Id));
    }

    private void AddConditionSteps(AutoCookerComponent comp, ReactionPrototype reaction, List<AutoCookStep> steps)
    {
        foreach (var category in reaction.MixingCategories ?? [])
        {
            var action = Loc.GetString(_proto.Index(category).VerbText);
            steps.Add(ConditionStep(comp, "autocook-step-mixing", ("action", action)));
        }

        if (reaction.MinimumTemperature > RoomTemperature + 1f)
            steps.Add(ConditionStep(comp, "autocook-step-heat", ("temp", MathF.Round(reaction.MinimumTemperature))));

        if (reaction.MaximumTemperature < RoomTemperature - 1f)
            steps.Add(ConditionStep(comp, "autocook-step-cool", ("temp", MathF.Round(reaction.MaximumTemperature))));
    }

    private AutoCookStep ConditionStep(AutoCookerComponent comp, string locId, (string, object) arg)
    {
        return new AutoCookStep
        {
            Kind = AutoCookStepKind.Condition,
            Text = Loc.GetString(locId, arg),
            Duration = comp.ReactionSeconds * 0.5f,
        };
    }

    private bool PlanUsesRestricted(AutoCookerComponent comp, List<AutoCookStep> steps)
    {
        return steps.SelectMany(step => step.Produce.Keys)
            .Any(reagent => IsRestrictedReagent(reagent) || IsExcludedReagent(comp, reagent));
    }

    private bool TryPlanMeal(AutoCookerComponent comp, FoodRecipePrototype recipe, KitchenPlan plan, int depth = 0)
    {
        if (depth > MaxMealDepth)
            return false;

        var work = plan.Branch();

        foreach (var (solid, count) in recipe.IngredientsSolids.Where(pair => !IsDishware(pair.Key)))
        {
            for (var i = 0; i < count.Int(); i++)
            {
                if (work.Items.GetValueOrDefault(solid) > 0)
                    work.TakeSolid(solid);
                else if (!TryProduceSolid(comp, solid, recipe, work, depth))
                    return false;
            }
        }

        foreach (var (reagent, quantity) in recipe.IngredientsReagents)
        {
            var amount = quantity.Float();
            if (!work.Has(reagent, amount))
                return false;

            work.Reagents[reagent] -= amount;
            work.ConsumeReagent(reagent, amount);
        }

        work.Steps.Add(new AutoCookStep
        {
            Kind = AutoCookStepKind.Cook,
            Text = Loc.GetString("autocook-step-cook", ("name", MealName(recipe))),
            Duration = MathF.Max(comp.PrepareSeconds, MathF.Min(recipe.CookTime, MaxCookSeconds) * 0.5f),
        });

        plan.Adopt(work);
        return true;
    }

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
        foreach (var sub in _meals.Where(meal => meal.Result == solid && meal != exclude))
        {
            var branch = plan.Branch();
            if (!TryPlanMeal(comp, sub, branch, depth + 1))
                continue;

            plan.Adopt(branch);
            return true;
        }

        return false;
    }

    private bool TryProduceByMix(AutoCookerComponent comp, string solid, KitchenPlan plan)
    {
        return _mixByEntity.TryGetValue(solid, out var mixes)
               && mixes.Any(mix => TryApplyMix(comp, mix.Reaction, solid, mix.Number, plan));
    }

    private bool TryApplyMix(AutoCookerComponent comp, ReactionPrototype reaction, string solid, int number, KitchenPlan plan)
    {
        if (!reaction.Reactants.All(pair => plan.Has(pair.Key, pair.Value.Amount.Float())))
            return false;

        foreach (var (reactant, data) in reaction.Reactants.Where(pair => !pair.Value.Catalyst))
        {
            var amount = data.Amount.Float();
            plan.Reagents[reactant] -= amount;
            plan.ConsumeReagent(reactant, amount);
        }

        plan.Steps.Add(PrepareStep("autocook-step-mix", solid, comp.PrepareSeconds));

        if (number > 1)
            plan.Items[solid] = plan.Items.GetValueOrDefault(solid) + number - 1;

        return true;
    }

    private bool TryProduceBySlice(AutoCookerComponent comp, string solid, FoodRecipePrototype? exclude, KitchenPlan plan, int depth)
    {
        if (!_sliceSources.TryGetValue(solid, out var wholes))
            return false;

        foreach (var (whole, count) in wholes.Where(source => source.Whole != solid))
        {
            var branch = plan.Branch();
            if (!TryTakeOrProduce(comp, whole, exclude, branch, depth + 1))
                continue;

            branch.Steps.Add(PrepareStep("autocook-step-slice", solid, comp.PrepareSeconds * 0.5f));

            if (count > 1)
                branch.Items[solid] = branch.Items.GetValueOrDefault(solid) + count - 1;

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

            branch.Steps.Add(PrepareStep("autocook-step-process", solid, comp.PrepareSeconds));
            plan.Adopt(branch);
            return true;
        }

        return false;
    }

    private bool TryTakeOrProduce(AutoCookerComponent comp, string solid, FoodRecipePrototype? exclude, KitchenPlan plan, int depth)
    {
        if (plan.Items.GetValueOrDefault(solid) <= 0)
            return TryProduceSolid(comp, solid, exclude, plan, depth);

        plan.TakeSolid(solid);
        return true;
    }

    private AutoCookStep PrepareStep(string locId, string solid, float duration)
    {
        return new AutoCookStep
        {
            Kind = AutoCookStepKind.Prepare,
            Text = Loc.GetString(locId, ("name", EntityName(solid))),
            Duration = duration,
        };
    }

    private string MealName(FoodRecipePrototype recipe)
    {
        return EntityName(recipe.Result);
    }
}
