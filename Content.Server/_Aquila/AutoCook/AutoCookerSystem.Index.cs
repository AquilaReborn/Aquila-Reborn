using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Nutrition.Components;
using Content.Shared._Aquila.AutoCook;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Construction.NodeEntities;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Construction.Steps;
using Content.Shared.Contraband;
using Content.Shared.EntityEffects.Effects.EntitySpawning;
using Content.Shared.Fluids.Components;
using Content.Shared.Kitchen;
using Content.Shared.Nutrition.Components;
using Content.Shared.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Aquila.AutoCook;

/// <summary>
/// Кэш прототипов, по которым строятся планы: реакции, нарезка, обработка инструментами и рецепты блюд.
/// </summary>
public sealed partial class AutoCookerSystem
{
    private static readonly ProtoId<MetabolismGroupPrototype> NarcoticMetabolism = "Narcotic";

    private readonly Dictionary<ProtoId<ReagentPrototype>, List<ReactionPrototype>> _reactionsByProduct = new();
    private readonly Dictionary<EntProtoId, List<KitchenMix>> _mixesByEntity = new();
    private readonly List<KitchenMix> _mixes = new();
    private readonly Dictionary<EntProtoId, List<(EntProtoId Whole, int Count)>> _sliceSources = new();
    private readonly Dictionary<EntProtoId, List<EntProtoId>> _transformSources = new();
    private readonly List<FoodRecipePrototype> _meals = new();
    private readonly HashSet<EntProtoId> _usedSolids = new();
    private readonly HashSet<ProtoId<ReagentPrototype>> _usedReagents = new();
    private readonly Dictionary<string, bool> _restrictedReagents = new();
    private readonly Dictionary<string, bool> _edibleSolids = new();
    private readonly Dictionary<string, List<AutoCookRecipeEntry>> _reagentEntries = new();
    private readonly Dictionary<EntityUid, BufferEntries> _bufferEntries = new();
    private bool _indexed;

    /// <summary>
    /// Список рецептов машины для конкретного содержимого буфера.
    /// </summary>
    private sealed record BufferEntries(Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Buffer, List<AutoCookRecipeEntry> Entries);

    /// <summary>
    /// Реакция, при которой из реагентов появляется съедобная сущность, например тесто.
    /// </summary>
    private sealed record KitchenMix(ReactionPrototype Reaction, EntProtoId Entity, int Number);

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>()
            || args.WasModified<ReagentPrototype>()
            || args.WasModified<ReactionPrototype>()
            || args.WasModified<FoodRecipePrototype>()
            || args.WasModified<ConstructionGraphPrototype>())
            _indexed = false;
    }

    private void EnsureIndex()
    {
        if (_indexed)
            return;

        _indexed = true;

        _reactionsByProduct.Clear();
        _mixesByEntity.Clear();
        _mixes.Clear();
        _sliceSources.Clear();
        _transformSources.Clear();
        _meals.Clear();
        _usedSolids.Clear();
        _usedReagents.Clear();
        _restrictedReagents.Clear();
        _edibleSolids.Clear();
        _reagentEntries.Clear();
        _bufferEntries.Clear();

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
                _reactionsByProduct.GetOrNew(product).Add(reaction);
            }

            if (reaction.Reactants.Count == 0 || reaction.Reactants.Keys.Any(IsRestrictedReagent))
                continue;

            var spawn = reaction.Effects
                .OfType<SpawnEntity>()
                .FirstOrDefault(effect => IsEdibleSolid(effect.Entity) && !IsRestrictedEntity(effect.Entity));

            if (spawn == null)
                continue;

            var mix = new KitchenMix(reaction, spawn.Entity, spawn.Number);
            _mixesByEntity.GetOrNew(spawn.Entity).Add(mix);
            _mixes.Add(mix);
        }
    }

    private void IndexSliceable()
    {
        foreach (var proto in _proto.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract
                || !proto.TryGetComponent<SliceableFoodComponent>(out var sliceable, EntityManager.ComponentFactory)
                || sliceable.Slice is not { } slice)
                continue;

            _sliceSources.GetOrNew(slice).Add((proto.ID, sliceable.TotalCount));
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

                    var sources = _transformSources.GetOrNew(target);
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
                || IsRestrictedMeal(meal))
                continue;

            _meals.Add(meal);

            if (IsEdibleSolid(meal.Result))
                _usedSolids.Add(meal.Result);

            foreach (var solid in meal.IngredientsSolids.Keys)
            {
                _usedSolids.Add(solid);
            }

            foreach (var reagent in meal.IngredientsReagents.Keys)
            {
                _usedReagents.Add(reagent);
            }
        }

        foreach (var mix in _mixes)
        {
            foreach (var reagent in mix.Reaction.Reactants.Keys)
            {
                _usedReagents.Add(reagent);
            }
        }
    }

    private bool IsRestrictedMeal(FoodRecipePrototype meal)
    {
        return IsRestrictedEntity(meal.Result)
               || meal.IngredientsSolids.Keys.Any(solid => IsEdibleSolid(solid) && IsRestrictedEntity(solid))
               || meal.IngredientsReagents.Keys.Any(IsRestrictedReagent);
    }

    private bool IsRestrictedEntity(string id)
    {
        return _proto.TryIndex<EntityPrototype>(id, out var proto)
               && proto.HasComponent<ContrabandComponent>(EntityManager.ComponentFactory);
    }

    /// <summary>
    /// Контрабандные, ведомственные и наркотические реагенты машины не готовят никогда.
    /// </summary>
    private bool IsRestrictedReagent(string id)
    {
        if (_restrictedReagents.TryGetValue(id, out var cached))
            return cached;

        var restricted = _proto.TryIndex<ReagentPrototype>(id, out var proto)
                         && (proto.ContrabandSeverity != null
                             || proto.AllowedDepartments.Count > 0
                             || proto.AllowedJobs.Count > 0
                             || proto.Metabolisms?.ContainsKey(NarcoticMetabolism) == true);

        _restrictedReagents[id] = restricted;
        return restricted;
    }

    private static bool IsForbiddenReagent(AutoCookerComponent comp, ProtoId<ReagentPrototype> id)
    {
        return comp.ExcludedReagents.Contains(id);
    }

    private static bool HasAnyMetabolism(ReagentPrototype proto, List<ProtoId<MetabolismGroupPrototype>> groups)
    {
        return proto.Metabolisms != null && proto.Metabolisms.Keys.Any(groups.Contains);
    }

    /// <summary>
    /// Твёрдая еда, а не ёмкость с жидкостью, которую тоже можно съесть или выпить.
    /// </summary>
    private bool IsEdibleSolid(string id)
    {
        if (_edibleSolids.TryGetValue(id, out var cached))
            return cached;

        var factory = EntityManager.ComponentFactory;
        var edible = _proto.TryIndex<EntityPrototype>(id, out var proto)
                     && proto.HasComponent<EdibleComponent>(factory)
                     && !proto.HasComponent<SolutionTransferComponent>(factory)
                     && !proto.HasComponent<MixableSolutionComponent>(factory)
                     && !proto.HasComponent<SpillableComponent>(factory)
                     && !proto.HasComponent<DrainableSolutionComponent>(factory);

        _edibleSolids[id] = edible;
        return edible;
    }

    private bool IsBaseReagent(AutoCookerComponent comp, ProtoId<ReagentPrototype> id)
    {
        if (comp.BaseReagents.Contains(id))
            return true;

        return !_reactionsByProduct.ContainsKey(id)
               && _proto.TryIndex(id, out var proto)
               && HasAnyMetabolism(proto, comp.BaseMetabolisms);
    }

    private bool IsCraftableTarget(AutoCookerComponent comp, ReagentPrototype proto)
    {
        return (comp.TargetGroups.Contains(proto.Group) || HasAnyMetabolism(proto, comp.TargetMetabolisms))
               && !IsRestrictedReagent(proto.ID)
               && !IsForbiddenReagent(comp, proto.ID)
               && !IsBaseReagent(comp, proto.ID)
               && _reactionsByProduct.ContainsKey(proto.ID);
    }
}
