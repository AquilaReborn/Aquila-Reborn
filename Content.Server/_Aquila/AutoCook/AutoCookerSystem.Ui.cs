using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Aquila.AutoCook;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Kitchen;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.AutoCook;

/// <summary>
/// Состояние интерфейса. Сервер отдаёт только данные, названия и тексты этапов локализует клиент.
/// </summary>
public sealed partial class AutoCookerSystem
{
    /// <summary>
    /// Объём, для которого строится превью рецепта реагента.
    /// </summary>
    private static readonly FixedPoint2 PreviewAmount = 10;

    private const string AlcoholicGroup = "AlcoholicDrinks";
    private static readonly ProtoId<MetabolismGroupPrototype> AlcoholMetabolism = "Alcohol";

    private const int MediumMaxReactions = 3;
    private const string MealGroupPrefix = "autocook-group-";
    private const string OtherGroup = "autocook-group-other";
    private const string DoughGroup = "autocook-group-dough";

    private void UpdateUi(Entity<AutoCookerComponent> ent)
    {
        ent.Comp.UiDirty = true;
    }

    private void FlushUi(Entity<AutoCookerComponent> ent, TimeSpan now)
    {
        if (!ent.Comp.UiDirty || now < ent.Comp.NextUiUpdate)
            return;

        ent.Comp.UiDirty = false;

        if (!_ui.IsUiOpen(ent.Owner, AutoCookerUiKey.Key))
            return;

        ent.Comp.NextUiUpdate = now + ent.Comp.UiUpdateInterval;
        SendUiState(ent);
    }

    private void SendUiState(Entity<AutoCookerComponent> ent)
    {
        EnsureIndex();

        if (ent.Comp.Kind == AutoCookKind.Kitchen)
        {
            var inventory = CreateKitchenPlan(ent);
            SetUiState(ent, GetKitchenEntries(ent, inventory), null, BuildStock(ent.Comp, inventory));
            return;
        }

        var buffer = GetBufferContents(ent);
        var stock = buffer
            .Where(pair => pair.Value > FixedPoint2.Zero)
            .Select(pair => new AutoCookStockEntry(pair.Key, true, pair.Value))
            .ToList();

        SetUiState(ent, GetReagentEntries(ent, buffer), BuildOutputInfo(ent), stock);
    }

    private void SetUiState(
        Entity<AutoCookerComponent> ent,
        List<AutoCookRecipeEntry> recipes,
        AutoCookOutputInfo? output,
        List<AutoCookStockEntry> stock)
    {
        SendRecipes(ent, recipes);

        var state = new AutoCookerBoundUserInterfaceState(
            ent.Comp.Kind,
            _power.IsPowered(ent.Owner),
            BuildJobInfo(ent.Comp.Job),
            ent.Comp.Queue.Select(order => new AutoCookQueueEntry(order.Recipe.Kind, GetResult(order.Recipe), order.Amount)).ToList(),
            ent.Comp.MaxQueue,
            output,
            stock,
            ent.Comp.BufferSolution != null);

        _ui.SetUiState(ent.Owner, AutoCookerUiKey.Key, state);
    }

    private void SendRecipes(Entity<AutoCookerComponent> ent, List<AutoCookRecipeEntry> recipes)
    {
        if (!ReferenceEquals(recipes, ent.Comp.SentRecipes))
        {
            ent.Comp.SentRecipes = recipes;
            ent.Comp.RecipeViewers.Clear();
        }

        AutoCookerRecipesEvent? ev = null;
        foreach (var actor in _ui.GetActors(ent.Owner, AutoCookerUiKey.Key))
        {
            if (!TryComp<ActorComponent>(actor, out var actorComp) || !ent.Comp.RecipeViewers.Add(actor))
                continue;

            ev ??= new AutoCookerRecipesEvent(GetNetEntity(ent), recipes);
            RaiseNetworkEvent(ev, actorComp.PlayerSession);
        }
    }

    private void OnUiClosed(Entity<AutoCookerComponent> ent, ref BoundUIClosedEvent args)
    {
        if (AutoCookerUiKey.Key.Equals(args.UiKey))
            ent.Comp.RecipeViewers.Remove(args.Actor);
    }

    /// <summary>
    /// Список зависит только от настроек прототипа и содержимого буфера.
    /// Пустой буфер кэшируется на прототип, заполненный - на машину, пока буфер не изменится.
    /// </summary>
    private List<AutoCookRecipeEntry> GetReagentEntries(
        Entity<AutoCookerComponent> ent,
        Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> buffer)
    {
        var key = MetaData(ent).EntityPrototype?.ID ?? string.Empty;
        var cacheable = buffer.Count == 0;
        if (cacheable)
        {
            _bufferEntries.Remove(ent.Owner);
            if (_reagentEntries.TryGetValue(key, out var cached))
                return cached;
        }
        else if (_bufferEntries.TryGetValue(ent.Owner, out var bufferCached) && SameContents(bufferCached.Buffer, buffer))
        {
            return bufferCached.Entries;
        }

        var entries = new List<AutoCookRecipeEntry>();

        foreach (var proto in _proto.EnumeratePrototypes<ReagentPrototype>())
        {
            if (IsListedReagent(ent.Comp, proto) && BuildReagentEntry(ent.Comp, proto, buffer) is { } entry)
                entries.Add(entry);
        }

        if (cacheable)
            _reagentEntries[key] = entries;
        else
            _bufferEntries[ent.Owner] = new BufferEntries(buffer, entries);

        return entries;
    }

    private static bool SameContents<TKey, TValue>(Dictionary<TKey, TValue> a, Dictionary<TKey, TValue> b)
        where TKey : notnull
    {
        if (a.Count != b.Count)
            return false;

        foreach (var (key, value) in a)
        {
            if (!b.TryGetValue(key, out var other) || !EqualityComparer<TValue>.Default.Equals(value, other))
                return false;
        }

        return true;
    }

    private List<AutoCookRecipeEntry> GetKitchenEntries(Entity<AutoCookerComponent> ent, KitchenPlan inventory)
    {
        if (_kitchenEntries.TryGetValue(ent.Owner, out var cached)
            && SameContents(cached.Items, inventory.Items)
            && SameContents(cached.Reagents, inventory.Reagents))
            return cached.Entries;

        var entries = BuildKitchenEntries(ent.Comp, inventory);
        _kitchenEntries[ent.Owner] = new KitchenEntries(
            new Dictionary<EntProtoId, int>(inventory.Items),
            new Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>(inventory.Reagents),
            entries);

        return entries;
    }

    private AutoCookRecipeEntry? BuildReagentEntry(
        AutoCookerComponent comp,
        ReagentPrototype proto,
        Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> buffer)
    {
        var id = new AutoCookRecipeId(AutoCookRecipeKind.Reagent, proto.ID);

        if (TryPlanTarget(comp, proto.ID, PreviewAmount, buffer, false, out var plan))
        {
            var ingredients = plan.Steps
                .LastOrDefault(step => step.Data.Kind == AutoCookStepKind.React)?.Consume
                .Select(pair => new AutoCookIngredient(pair.Key, true, null, pair.Value, true))
                .ToList() ?? [];

            return new AutoCookRecipeEntry(
                id,
                proto.ID,
                GetReagentGroup(comp, proto, plan.Steps),
                true,
                ingredients,
                plan.Steps.Select(step => step.Data).ToList());
        }

        // Сама машина реагент не сделает: показываем последнюю реакцию и что для неё нужно подать в буфер
        if (!TryGetLastReaction(comp, proto.ID, out var reaction))
            return null;

        var batches = GetBatches(PreviewAmount, reaction.Products[proto.ID]);
        var missing = reaction.Reactants
            .Select(pair =>
            {
                var need = pair.Value.Amount * batches;
                var have = buffer.GetValueOrDefault(pair.Key);
                return new AutoCookIngredient(pair.Key, true, have, need, have >= need || IsBaseReagent(comp, pair.Key));
            })
            .ToList();

        var steps = new List<AutoCookStep>();
        AddConditionSteps(comp, reaction, steps);
        steps.Add(ReactStep(comp, reaction, proto.ID, batches));

        var group = TryPlanTarget(comp, proto.ID, PreviewAmount, buffer, true, out var full)
            ? GetReagentGroup(comp, proto, full.Steps)
            : "autocook-group-complex";

        return new AutoCookRecipeEntry(id, proto.ID, group, false, missing, steps.Select(step => step.Data).ToList());
    }

    /// <summary>
    /// Реакция, которой машина получила бы реагент, если бы ей подали все реактивы.
    /// </summary>
    private bool TryGetLastReaction(AutoCookerComponent comp, string reagent, [NotNullWhen(true)] out ReactionPrototype? reaction)
    {
        reaction = null;
        if (!_reactionsByProduct.TryGetValue(reagent, out var reactions))
            return false;

        reaction = reactions.FirstOrDefault(candidate =>
            candidate.Products[reagent] > 0
            && CanMix(comp, candidate)
            && !candidate.Reactants.Keys.Any(id => IsRestrictedReagent(id) || IsForbiddenReagent(comp, id)));

        return reaction != null;
    }

    private List<AutoCookRecipeEntry> BuildKitchenEntries(AutoCookerComponent comp, KitchenPlan inventory)
    {
        var entries = new List<AutoCookRecipeEntry>();

        foreach (var meal in _meals)
        {
            if (HasRealIngredients(comp, meal))
                entries.Add(BuildMealEntry(comp, meal, inventory));
        }

        foreach (var mix in _mixes)
        {
            entries.Add(BuildMixEntry(comp, mix, inventory));
        }

        foreach (var (target, sources) in _transformSources)
        {
            entries.Add(BuildMakeEntry(comp, target, sources, inventory));
        }

        return entries;
    }

    private AutoCookRecipeEntry BuildMealEntry(AutoCookerComponent comp, FoodRecipePrototype meal, KitchenPlan inventory)
    {
        var plan = inventory.Branch();
        var available = TryPlanMeal(comp, meal, plan, 0);

        var ingredients = new List<AutoCookIngredient>();
        foreach (var (solid, count) in meal.IngredientsSolids)
        {
            if (IsFreeIngredient(comp, solid))
                continue;

            var have = inventory.Items.GetValueOrDefault(solid);
            var ok = have >= count.Int() || CanProduceSolid(meal, solid);
            ingredients.Add(new AutoCookIngredient(solid, false, have, count, ok));
        }

        foreach (var (reagent, quantity) in meal.IngredientsReagents)
        {
            ingredients.Add(new AutoCookIngredient(reagent, true, inventory.Reagents.GetValueOrDefault(reagent), quantity, inventory.Has(reagent, quantity)));
        }

        var steps = available
            ? plan.Steps.Select(step => step.Data).ToList()
            : [new AutoCookStepData(AutoCookStepKind.Cook, meal.Result, 0f, TimeSpan.Zero)];

        return new AutoCookRecipeEntry(
            new AutoCookRecipeId(AutoCookRecipeKind.Meal, meal.ID),
            meal.Result,
            GetMealGroup(meal),
            available,
            ingredients,
            steps);
    }

    private static AutoCookRecipeEntry BuildMixEntry(AutoCookerComponent comp, KitchenMix mix, KitchenPlan inventory)
    {
        var ingredients = mix.Reaction.Reactants
            .Select(pair => new AutoCookIngredient(
                pair.Key,
                true,
                inventory.Reagents.GetValueOrDefault(pair.Key),
                pair.Value.Amount,
                inventory.Has(pair.Key, pair.Value.Amount)))
            .ToList();

        return new AutoCookRecipeEntry(
            new AutoCookRecipeId(AutoCookRecipeKind.Mix, mix.Reaction.ID),
            mix.Entity,
            DoughGroup,
            ingredients.All(ingredient => ingredient.Ok),
            ingredients,
            [new AutoCookStepData(AutoCookStepKind.Mix, mix.Entity, 0f, comp.PrepareDuration)]);
    }

    private AutoCookRecipeEntry BuildMakeEntry(AutoCookerComponent comp, EntProtoId target, List<EntProtoId> sources, KitchenPlan inventory)
    {
        var plan = inventory.Branch();
        var available = TryProduceSolid(comp, target, null, plan, 0);

        var ingredients = sources
            .Select(source =>
            {
                var have = inventory.Items.GetValueOrDefault(source);
                return new AutoCookIngredient(source, false, have, 1, have >= 1 || CanProduceSolid(null, source));
            })
            .ToList();

        var steps = available
            ? plan.Steps.Select(step => step.Data).ToList()
            : [new AutoCookStepData(AutoCookStepKind.Process, target, 0f, TimeSpan.Zero)];

        return new AutoCookRecipeEntry(
            new AutoCookRecipeId(AutoCookRecipeKind.Make, target),
            target,
            DoughGroup,
            available,
            ingredients,
            steps);
    }

    /// <summary>
    /// Прототип результата заказа, по нему клиент показывает название.
    /// </summary>
    private string GetResult(AutoCookRecipeId recipe)
    {
        return recipe.Kind switch
        {
            AutoCookRecipeKind.Meal => _meals.FirstOrDefault(meal => meal.ID == recipe.Id)?.Result ?? recipe.Id,
            AutoCookRecipeKind.Mix => _mixes.FirstOrDefault(mix => mix.Reaction.ID == recipe.Id)?.Entity ?? recipe.Id,
            _ => recipe.Id,
        };
    }

    private string GetResultName(AutoCookRecipeId recipe)
    {
        var result = GetResult(recipe);

        if (recipe.Kind == AutoCookRecipeKind.Reagent)
            return _proto.TryIndex<ReagentPrototype>(result, out var reagent) ? reagent.LocalizedName : result;

        return _proto.TryIndex<EntityPrototype>(result, out var entity) ? entity.Name : result;
    }

    private string GetMealGroup(FoodRecipePrototype meal)
    {
        var key = MealGroupPrefix + meal.Group.ToLowerInvariant();
        return Loc.TryGetString(key, out _) ? key : OtherGroup;
    }

    private static string GetReagentGroup(AutoCookerComponent comp, ReagentPrototype proto, List<AutoCookStep> steps)
    {
        if (comp.Kind == AutoCookKind.Bar)
        {
            var alcoholic = proto.Group == AlcoholicGroup || proto.Metabolisms?.ContainsKey(AlcoholMetabolism) == true;
            return alcoholic ? "autocook-group-alcohol" : "autocook-group-drink";
        }

        return CountReactions(steps) switch
        {
            0 => "autocook-group-elements",
            1 => "autocook-group-simple",
            <= MediumMaxReactions => "autocook-group-medium",
            _ => "autocook-group-complex",
        };
    }

    private bool CanProduceSolid(FoodRecipePrototype? recipe, string solid)
    {
        return _meals.Any(meal => meal.Result == solid && meal != recipe)
               || _mixesByEntity.ContainsKey(solid)
               || _sliceSources.ContainsKey(solid)
               || _transformSources.ContainsKey(solid);
    }

    private static AutoCookJobInfo? BuildJobInfo(AutoCookJob? job)
    {
        if (job == null)
            return null;

        return new AutoCookJobInfo(
            job.Recipe.Kind,
            job.Result,
            job.Steps.Select(step => step.Data).ToList(),
            job.StepIndex,
            job.StepStart,
            job.PausedElapsed,
            job.WaitingOutput);
    }

    private AutoCookOutputInfo? BuildOutputInfo(Entity<AutoCookerComponent> ent)
    {
        if (_itemSlots.GetItemOrNull(ent, AutoCookerComponent.BeakerSlotId) is not { } beaker
            || !_solution.TryGetFitsInDispenser(beaker, out _, out var solution))
            return null;

        return new AutoCookOutputInfo(Name(beaker), solution.Volume, solution.MaxVolume, solution.GetColor(_proto));
    }

    private List<AutoCookStockEntry> BuildStock(AutoCookerComponent comp, KitchenPlan inventory)
    {
        var stock = new List<AutoCookStockEntry>();

        foreach (var (id, count) in inventory.Items)
        {
            if (_usedSolids.Contains(id) && !IsFreeIngredient(comp, id))
                stock.Add(new AutoCookStockEntry(id, false, count));
        }

        foreach (var (id, amount) in inventory.Reagents)
        {
            if (amount > FixedPoint2.Zero && _usedReagents.Contains(id))
                stock.Add(new AutoCookStockEntry(id, true, amount));
        }

        return stock;
    }
}
