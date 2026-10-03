using System.Linq;
using Content.Server.Nutrition.Components;
using Content.Shared._Aquila.AutoCook;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Construction.NodeEntities;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Construction.Steps;
using Content.Shared.EntityEffects.Effects.EntitySpawning;
using Content.Shared.Kitchen;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.AutoCook;

public sealed partial class AutoCookerSystem
{
    private const string NarcoticGroup = "Narcotic";

    private static readonly string[] ContainerComponents =
    [
        "SolutionTransfer",
        "MixableSolution",
        "Spillable",
        "DrainableSolution",
    ];

    private readonly Dictionary<string, List<ReactionPrototype>> _reactionsByProduct = new();
    private readonly Dictionary<string, List<(ReactionPrototype Reaction, int Number)>> _mixByEntity = new();
    private readonly List<(ReactionPrototype Reaction, string Entity, int Number)> _mixList = new();
    private readonly Dictionary<string, List<(string Whole, int Count)>> _sliceSources = new();
    private readonly Dictionary<string, List<string>> _transformSources = new();
    private readonly List<FoodRecipePrototype> _meals = new();
    private readonly HashSet<string> _usedSolids = new();
    private readonly HashSet<string> _usedReagents = new();
    private readonly Dictionary<string, bool> _restrictedReagents = new();
    private readonly Dictionary<string, bool> _edibleSolids = new();
    private readonly Dictionary<AutoCookKind, List<AutoCookRecipeEntry>> _reagentEntries = new();
    private bool _indexed;

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        _indexed = false;
    }

    private void EnsureIndex()
    {
        if (_indexed)
            return;

        _indexed = true;

        _reactionsByProduct.Clear();
        _mixByEntity.Clear();
        _mixList.Clear();
        _sliceSources.Clear();
        _transformSources.Clear();
        _meals.Clear();
        _usedSolids.Clear();
        _usedReagents.Clear();
        _restrictedReagents.Clear();
        _edibleSolids.Clear();
        _reagentEntries.Clear();

        IndexReactions();
        IndexSliceable();
        IndexTransforms();
        IndexMeals();
    }

    private void IndexReactions()
    {
        foreach (var reaction in _proto.EnumeratePrototypes<ReactionPrototype>())
        {
            foreach (var product in reaction.Products.Keys)
            {
                if (!_reactionsByProduct.TryGetValue(product, out var list))
                    _reactionsByProduct[product] = list = new List<ReactionPrototype>();

                list.Add(reaction);
            }

            if (reaction.Reactants.Count == 0 || reaction.Reactants.Keys.Any(IsRestrictedReagent))
                continue;

            foreach (var effect in reaction.Effects)
            {
                if (effect is not SpawnEntity spawn
                    || !IsEdibleSolid(spawn.Entity.Id)
                    || IsRestrictedEntity(spawn.Entity.Id))
                    continue;

                if (!_mixByEntity.TryGetValue(spawn.Entity.Id, out var list))
                    _mixByEntity[spawn.Entity.Id] = list = new List<(ReactionPrototype, int)>();

                list.Add((reaction, spawn.Number));
                _mixList.Add((reaction, spawn.Entity.Id, spawn.Number));
                break;
            }
        }
    }

    private void IndexSliceable()
    {
        foreach (var proto in _proto.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract
                || !proto.Components.TryGetValue("SliceableFood", out var entry)
                || entry.Component is not SliceableFoodComponent sliceable
                || sliceable.Slice is not { } slice)
                continue;

            if (!_sliceSources.TryGetValue(slice.Id, out var wholes))
                _sliceSources[slice.Id] = wholes = new List<(string, int)>();

            wholes.Add((proto.ID, sliceable.TotalCount));
        }
    }

    private void IndexTransforms()
    {
        foreach (var graph in _proto.EnumeratePrototypes<ConstructionGraphPrototype>())
        {
            foreach (var node in graph.Nodes.Values)
            {
                if (node.Entity is not StaticNodeEntity { Id: { } source } || !IsEdibleSolid(source))
                    continue;

                foreach (var edge in node.Edges)
                {
                    if (edge.Steps.Count == 0
                        || edge.Steps.Any(step => step is not ToolConstructionGraphStep)
                        || !graph.Nodes.TryGetValue(edge.Target, out var targetNode)
                        || targetNode.Entity is not StaticNodeEntity { Id: { } target }
                        || target == source
                        || !IsEdibleSolid(target)
                        || IsRestrictedEntity(target))
                        continue;

                    if (!_transformSources.TryGetValue(target, out var sources))
                        _transformSources[target] = sources = new List<string>();

                    if (!sources.Contains(source))
                        sources.Add(source);
                }
            }
        }
    }

    private void IndexMeals()
    {
        foreach (var meal in _proto.EnumeratePrototypes<FoodRecipePrototype>())
        {
            if (meal.SecretRecipe
                || string.IsNullOrEmpty(meal.Result)
                || IsRestrictedMeal(meal)
                || !HasRealIngredients(meal))
                continue;

            _meals.Add(meal);
        }

        foreach (var meal in _meals)
        {
            if (IsEdibleSolid(meal.Result))
                _usedSolids.Add(meal.Result);

            foreach (var solid in meal.IngredientsSolids.Keys.Where(solid => !IsDishware(solid)))
            {
                _usedSolids.Add(solid);
            }

            _usedReagents.UnionWith(meal.IngredientsReagents.Keys);
        }

        foreach (var (reaction, _, _) in _mixList)
        {
            _usedReagents.UnionWith(reaction.Reactants.Keys);
        }
    }

    private bool IsRestrictedMeal(FoodRecipePrototype meal)
    {
        return IsRestrictedEntity(meal.Result)
               || meal.IngredientsSolids.Keys.Any(solid => IsEdibleSolid(solid) && IsRestrictedEntity(solid))
               || meal.IngredientsReagents.Keys.Any(IsRestrictedReagent);
    }

    private bool HasRealIngredients(FoodRecipePrototype meal)
    {
        return meal.IngredientsReagents.Count > 0 || meal.IngredientsSolids.Keys.Any(solid => !IsDishware(solid));
    }

    private bool IsRestrictedEntity(string id)
    {
        return _proto.TryIndex<EntityPrototype>(id, out var proto) && proto.Components.ContainsKey("Contraband");
    }

    private bool IsRestrictedReagent(string id)
    {
        if (_restrictedReagents.TryGetValue(id, out var cached))
            return cached;

        var restricted = _proto.TryIndex<ReagentPrototype>(id, out var proto)
                         && (proto.ContrabandSeverity != null
                             || proto.AllowedDepartments.Count > 0
                             || proto.AllowedJobs.Count > 0
                             || HasMetabolism(proto, NarcoticGroup));

        _restrictedReagents[id] = restricted;
        return restricted;
    }

    private static bool IsExcludedReagent(AutoCookerComponent comp, string id)
    {
        return comp.ExcludedReagents.Any(excluded => excluded.Id == id);
    }

    private static bool HasMetabolism(ReagentPrototype proto, string group)
    {
        return proto.Metabolisms != null && proto.Metabolisms.Keys.Any(key => key.Id == group);
    }

    private static bool HasAnyMetabolism(ReagentPrototype proto, List<string> groups)
    {
        return proto.Metabolisms != null && proto.Metabolisms.Keys.Any(key => groups.Contains(key.Id));
    }

    private static bool IsDishware(string id)
    {
        return id.StartsWith("FoodPlate") || id.StartsWith("FoodBowl") || id.StartsWith("FoodTin");
    }

    private bool IsEdibleSolid(string id)
    {
        if (_edibleSolids.TryGetValue(id, out var cached))
            return cached;

        var edible = _proto.TryIndex<EntityPrototype>(id, out var proto)
                     && proto.Components.ContainsKey("Edible")
                     && !ContainerComponents.Any(proto.Components.ContainsKey);

        _edibleSolids[id] = edible;
        return edible;
    }

    private bool IsBaseReagent(AutoCookerComponent comp, string reagentId)
    {
        if (comp.BaseReagents.Any(baseReagent => baseReagent.Id == reagentId))
            return true;

        return !_reactionsByProduct.ContainsKey(reagentId)
               && _proto.TryIndex<ReagentPrototype>(reagentId, out var proto)
               && HasAnyMetabolism(proto, comp.BaseMetabolisms);
    }

    private static bool IsTargetReagent(AutoCookerComponent comp, ReagentPrototype proto)
    {
        return comp.TargetGroups.Contains(proto.Group) || HasAnyMetabolism(proto, comp.TargetMetabolisms);
    }

    private bool IsCraftableTarget(AutoCookerComponent comp, ReagentPrototype proto)
    {
        return IsTargetReagent(comp, proto)
               && !IsRestrictedReagent(proto.ID)
               && !IsExcludedReagent(comp, proto.ID)
               && !IsBaseReagent(comp, proto.ID)
               && _reactionsByProduct.ContainsKey(proto.ID);
    }

    private string ReagentName(string id)
    {
        return _proto.TryIndex<ReagentPrototype>(id, out var proto) ? proto.LocalizedName : id;
    }

    private string EntityName(string id)
    {
        return _proto.TryIndex<EntityPrototype>(id, out var proto) ? proto.Name : id;
    }

    private static string FormatAmount(float amount)
    {
        return MathF.Round(amount, 1).ToString("0.#");
    }
}
