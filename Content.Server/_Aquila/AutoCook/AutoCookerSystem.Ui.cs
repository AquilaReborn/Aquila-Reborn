using System.Linq;
using Content.Shared._Aquila.AutoCook;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Kitchen;

namespace Content.Server._Aquila.AutoCook;

public sealed partial class AutoCookerSystem
{
    private const float PreviewAmount = 10f;
    private const string AlcoholicGroup = "AlcoholicDrinks";

    private void UpdateUi(Entity<AutoCookerComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, AutoCookerUiKey.Key))
            return;

        EnsureIndex();

        var recipes = ent.Comp.Kind == AutoCookKind.Kitchen
            ? BuildKitchenEntries(ent)
            : BuildReagentEntries(ent);

        var state = new AutoCookerBoundUserInterfaceState(
            ent.Comp.Kind,
            _power.IsPowered(ent.Owner),
            recipes,
            BuildJobInfo(ent.Comp.Job),
            ent.Comp.Queue.Select(order => new AutoCookQueueEntry(GetRecipeName(order.RecipeId), order.Amount)).ToList(),
            ent.Comp.MaxQueue,
            BuildOutputInfo(ent),
            BuildStock(ent));

        _ui.SetUiState(ent.Owner, AutoCookerUiKey.Key, state);
    }

    private List<AutoCookRecipeEntry> BuildReagentEntries(Entity<AutoCookerComponent> ent)
    {
        if (_reagentEntries.TryGetValue(ent.Comp.Kind, out var cached))
            return cached;

        var entries = new List<AutoCookRecipeEntry>();

        foreach (var proto in _proto.EnumeratePrototypes<ReagentPrototype>().Where(proto => IsCraftableTarget(ent.Comp, proto)))
        {
            var raw = new List<AutoCookStep>();
            if (!TryPlanReagent(ent.Comp, proto.ID, PreviewAmount, raw) || PlanUsesRestricted(ent.Comp, raw))
                continue;

            var steps = FinalizeReagentSteps(ent.Comp, raw);
            if (IsTooComplex(ent.Comp, proto.ID, steps))
                continue;

            var ingredients = steps
                .LastOrDefault(step => step.Kind == AutoCookStepKind.React)?.Consume
                .Select(pair => new AutoCookIngredient(ReagentName(pair.Key), FormatAmount(pair.Value), true))
                .ToList() ?? new List<AutoCookIngredient>();

            entries.Add(new AutoCookRecipeEntry(
                AutoCookIds.Reagent + proto.ID,
                proto.LocalizedName,
                GetReagentGroup(ent.Comp, proto, steps),
                true,
                ingredients,
                steps.Select(step => step.Text).ToList()));
        }

        entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        _reagentEntries[ent.Comp.Kind] = entries;
        return entries;
    }

    private List<AutoCookRecipeEntry> BuildKitchenEntries(Entity<AutoCookerComponent> ent)
    {
        var (items, reagents) = GetKitchenInventory(ent);
        var entries = new List<AutoCookRecipeEntry>();

        entries.AddRange(_meals.Select(recipe => BuildMealEntry(ent, recipe, items, reagents)));
        entries.AddRange(_mixList.Select(mix => BuildMixEntry(mix.Reaction, mix.Entity, reagents)));
        entries.AddRange(_transformSources.Select(pair => BuildMakeEntry(ent, pair.Key, pair.Value, items, reagents)));

        entries.Sort((a, b) =>
        {
            var byAvailable = b.Available.CompareTo(a.Available);
            return byAvailable != 0
                ? byAvailable
                : string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
        });

        return entries;
    }

    private AutoCookRecipeEntry BuildMealEntry(
        Entity<AutoCookerComponent> ent,
        FoodRecipePrototype recipe,
        Dictionary<string, int> items,
        Dictionary<string, float> reagents)
    {
        var plan = new KitchenPlan(new Dictionary<string, int>(items), new Dictionary<string, float>(reagents));
        var available = TryPlanMeal(ent.Comp, recipe, plan);

        var ingredients = new List<AutoCookIngredient>();
        foreach (var (solid, count) in recipe.IngredientsSolids.Where(pair => !IsDishware(pair.Key)))
        {
            var have = items.GetValueOrDefault(solid);
            ingredients.Add(new AutoCookIngredient(
                EntityName(solid),
                $"{have}/{count.Int()}",
                have >= count.Int() || CanProduceSolid(recipe, solid)));
        }

        foreach (var (reagent, quantity) in recipe.IngredientsReagents)
        {
            var have = reagents.GetValueOrDefault(reagent);
            ingredients.Add(new AutoCookIngredient(
                ReagentName(reagent),
                $"{FormatAmount(have)}/{FormatAmount(quantity.Float())}",
                have + Epsilon >= quantity.Float()));
        }

        var steps = available
            ? plan.Steps.Select(step => step.Text).ToList()
            : [Loc.GetString("autocook-step-cook", ("name", MealName(recipe)))];

        return new AutoCookRecipeEntry(AutoCookIds.Meal + recipe.ID, MealName(recipe), GetMealGroup(recipe), available, ingredients, steps);
    }

    private AutoCookRecipeEntry BuildMixEntry(
        ReactionPrototype reaction,
        string entity,
        Dictionary<string, float> reagents)
    {
        var ingredients = reaction.Reactants
            .Select(pair => new AutoCookIngredient(
                ReagentName(pair.Key),
                $"{FormatAmount(reagents.GetValueOrDefault(pair.Key))}/{FormatAmount(pair.Value.Amount.Float())}",
                reagents.GetValueOrDefault(pair.Key) + Epsilon >= pair.Value.Amount.Float()))
            .ToList();

        return new AutoCookRecipeEntry(
            AutoCookIds.Mix + reaction.ID,
            EntityName(entity),
            Loc.GetString("autocook-group-dough"),
            ingredients.All(ingredient => ingredient.Ok),
            ingredients,
            [Loc.GetString("autocook-step-mix", ("name", EntityName(entity)))]);
    }

    private AutoCookRecipeEntry BuildMakeEntry(
        Entity<AutoCookerComponent> ent,
        string target,
        List<string> sources,
        Dictionary<string, int> items,
        Dictionary<string, float> reagents)
    {
        var plan = new KitchenPlan(new Dictionary<string, int>(items), new Dictionary<string, float>(reagents));
        var available = TryProduceSolid(ent.Comp, target, null, plan, 0);

        var ingredients = sources
            .Select(source => new AutoCookIngredient(
                EntityName(source),
                $"{items.GetValueOrDefault(source)}/1",
                items.GetValueOrDefault(source) >= 1 || CanProduceSolid(null, source)))
            .ToList();

        var steps = available
            ? plan.Steps.Select(step => step.Text).ToList()
            : [Loc.GetString("autocook-step-process", ("name", EntityName(target)))];

        return new AutoCookRecipeEntry(AutoCookIds.Make + target, EntityName(target), Loc.GetString("autocook-group-dough"), available, ingredients, steps);
    }

    private string GetRecipeName(string recipeId)
    {
        if (recipeId.StartsWith(AutoCookIds.Reagent))
            return ReagentName(recipeId[AutoCookIds.Reagent.Length..]);

        if (recipeId.StartsWith(AutoCookIds.Meal))
        {
            var meal = _meals.FirstOrDefault(m => m.ID == recipeId[AutoCookIds.Meal.Length..]);
            return meal == null ? recipeId : MealName(meal);
        }

        if (recipeId.StartsWith(AutoCookIds.Mix))
        {
            var mix = _mixList.FirstOrDefault(m => m.Reaction.ID == recipeId[AutoCookIds.Mix.Length..]);
            return mix.Entity == null ? recipeId : EntityName(mix.Entity);
        }

        return recipeId.StartsWith(AutoCookIds.Make) ? EntityName(recipeId[AutoCookIds.Make.Length..]) : recipeId;
    }

    private string GetMealGroup(FoodRecipePrototype recipe)
    {
        var key = "autocook-group-" + recipe.Group.ToLowerInvariant();
        return Loc.TryGetString(key, out var text) ? text : recipe.Group;
    }

    private string GetReagentGroup(AutoCookerComponent comp, ReagentPrototype proto, List<AutoCookStep> steps)
    {
        if (comp.Kind == AutoCookKind.Bar)
            return Loc.GetString(IsAlcoholic(proto) ? "autocook-group-alcohol" : "autocook-group-drink");

        var reactions = steps.Count(step => step.Kind == AutoCookStepKind.React);
        return Loc.GetString(reactions <= 1 ? "autocook-group-simple" : "autocook-group-medium");
    }

    private static bool IsAlcoholic(ReagentPrototype proto)
    {
        return proto.Group == AlcoholicGroup || HasMetabolism(proto, "Alcohol");
    }

    private bool CanProduceSolid(FoodRecipePrototype? recipe, string solid)
    {
        return _meals.Any(meal => meal.Result == solid && meal != recipe)
               || _mixByEntity.ContainsKey(solid)
               || _sliceSources.ContainsKey(solid)
               || _transformSources.ContainsKey(solid);
    }

    private AutoCookJobInfo? BuildJobInfo(AutoCookJob? job)
    {
        if (job == null)
            return null;

        var steps = job.Steps.Select((step, index) => new AutoCookStepInfo(
            step.Text,
            step.Duration,
            index < job.StepIndex
                ? AutoCookStepStatus.Done
                : index == job.StepIndex ? AutoCookStepStatus.Active : AutoCookStepStatus.Pending)).ToList();

        return new AutoCookJobInfo(
            job.RecipeName,
            steps,
            job.StepIndex,
            job.StepStart,
            job.PausedElapsed,
            job.Paused,
            job.WaitingOutput);
    }

    private AutoCookOutputInfo? BuildOutputInfo(Entity<AutoCookerComponent> ent)
    {
        if (ent.Comp.Kind == AutoCookKind.Kitchen)
            return null;

        var beaker = _itemSlots.GetItemOrNull(ent, AutoCookerComponent.BeakerSlotName);
        if (beaker == null || !_solution.TryGetFitsInDispenser(beaker.Value, out _, out var solution))
            return null;

        return new AutoCookOutputInfo(Name(beaker.Value), solution.Volume, solution.MaxVolume, solution.GetColor(_proto));
    }

    private List<AutoCookStockEntry> BuildStock(Entity<AutoCookerComponent> ent)
    {
        var stock = new List<AutoCookStockEntry>();
        if (ent.Comp.Kind != AutoCookKind.Kitchen)
            return stock;

        var (items, reagents) = GetKitchenInventory(ent);

        stock.AddRange(items
            .Where(pair => _usedSolids.Contains(pair.Key))
            .OrderBy(pair => EntityName(pair.Key))
            .Select(pair => new AutoCookStockEntry(EntityName(pair.Key), "×" + pair.Value)));

        stock.AddRange(reagents
            .Where(pair => pair.Value > 0.01f && _usedReagents.Contains(pair.Key))
            .OrderBy(pair => ReagentName(pair.Key))
            .Select(pair => new AutoCookStockEntry(ReagentName(pair.Key), FormatAmount(pair.Value))));

        return stock;
    }
}
