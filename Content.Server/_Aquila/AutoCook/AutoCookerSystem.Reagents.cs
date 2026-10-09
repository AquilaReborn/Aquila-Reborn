using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Aquila.AutoCook;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.AutoCook;

/// <summary>
/// Синтез реагентов для бара и химии: базовые реагенты берутся из энергии, готовые промежуточные из буфера,
/// остальное получается реакциями.
/// </summary>
public sealed partial class AutoCookerSystem
{
    private const int MaxReagentDepth = 6;

    /// <summary>
    /// Насколько температура реакции должна отличаться от комнатной, чтобы понадобился нагрев или охлаждение.
    /// </summary>
    private const float TemperatureTolerance = 1f;

    /// <summary>
    /// Черновик плана синтеза. Ветка создаётся на каждую попытку реакции и принимается, только если попытка удалась.
    /// </summary>
    private sealed class ReagentPlan(Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> buffer)
    {
        public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Buffer = buffer;
        public readonly List<AutoCookStep> Steps = new();
        public readonly Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> TakenFromBuffer = new();

        public ReagentPlan Branch()
        {
            return new ReagentPlan(new Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>(Buffer));
        }

        public void Adopt(ReagentPlan branch)
        {
            Buffer = branch.Buffer;
            Steps.AddRange(branch.Steps);

            foreach (var (id, amount) in branch.TakenFromBuffer)
            {
                TakenFromBuffer[id] = TakenFromBuffer.GetValueOrDefault(id) + amount;
            }
        }

        /// <summary>
        /// Забирает из буфера сколько есть, но не больше <paramref name="amount"/>, и возвращает забранное.
        /// </summary>
        public FixedPoint2 TakeFromBuffer(ProtoId<ReagentPrototype> reagent, FixedPoint2 amount)
        {
            var taken = FixedPoint2.Min(Buffer.GetValueOrDefault(reagent), amount);
            if (taken <= FixedPoint2.Zero)
                return FixedPoint2.Zero;

            Buffer[reagent] -= taken;
            TakenFromBuffer[reagent] = TakenFromBuffer.GetValueOrDefault(reagent) + taken;
            return taken;
        }

        /// <summary>
        /// Катализатор не расходуется: он должен лежать в буфере, но из буфера не забирается.
        /// </summary>
        public bool TryReserve(ProtoId<ReagentPrototype> reagent, FixedPoint2 amount)
        {
            if (Buffer.GetValueOrDefault(reagent) < amount)
                return false;

            Buffer[reagent] -= amount;
            return true;
        }
    }

    private AutoCookJob? TryCreateReagentJob(Entity<AutoCookerComponent> ent, AutoCookOrder order)
    {
        if (order.Recipe.Kind != AutoCookRecipeKind.Reagent)
            return null;

        var amount = FixedPoint2.Min(order.Amount, GetBeakerSpace(ent));
        if (amount < 1 || !TryPlanTarget(ent.Comp, order.Recipe.Id, amount, GetBufferContents(ent), false, out var plan))
            return null;

        if (!TryDrainBuffer(ent, plan.TakenFromBuffer))
            return null;

        return new AutoCookJob(order.Recipe, order.Recipe.Id, plan.Steps)
        {
            TargetAmount = amount,
            ConsumedReagents = plan.TakenFromBuffer,
        };
    }

    /// <summary>
    /// Реагент, который машина показывает в списке рецептов.
    /// </summary>
    private bool IsListedReagent(AutoCookerComponent comp, ReagentPrototype proto)
    {
        if (comp.ListBaseReagents && comp.BaseReagents.Contains(proto.ID))
            return !IsRestrictedReagent(proto.ID) && !IsForbiddenReagent(comp, proto.ID);

        return IsCraftableTarget(comp, proto);
    }

    /// <summary>
    /// Строит план синтеза реагента. Без <paramref name="ignoreLimit"/> учитывается лимит реакций машины.
    /// </summary>
    private bool TryPlanTarget(
        AutoCookerComponent comp,
        string reagent,
        FixedPoint2 amount,
        Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> buffer,
        bool ignoreLimit,
        [NotNullWhen(true)] out ReagentPlan? plan)
    {
        plan = null;

        if (!_proto.TryIndex<ReagentPrototype>(reagent, out var proto) || !IsListedReagent(comp, proto))
            return false;

        var draft = new ReagentPlan(new Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>(buffer));
        if (!TryPlanReagent(comp, draft, proto.ID, amount, 0)
            || draft.Steps.Any(step => step.Produce.Keys.Any(id => IsRestrictedReagent(id) || IsForbiddenReagent(comp, id))))
            return false;

        if (!ignoreLimit
            && comp.MaxReactions is { } maxReactions
            && CountReactions(draft.Steps) > maxReactions
            && !comp.ComplexWhitelist.Contains(proto.ID))
            return false;

        var steps = MergeSupplySteps(comp, draft.Steps);
        draft.Steps.Clear();
        draft.Steps.AddRange(steps);

        plan = draft;
        return plan.Steps.Count > 0;
    }

    private bool TryPlanReagent(
        AutoCookerComponent comp,
        ReagentPlan plan,
        ProtoId<ReagentPrototype> reagent,
        FixedPoint2 amount,
        int depth)
    {
        if (depth > MaxReagentDepth || amount <= 0)
            return false;

        // Сам результат из буфера не берём, иначе машина просто переливала бы его в стакан
        if (depth > 0)
        {
            var taken = plan.TakeFromBuffer(reagent, amount);
            if (taken > FixedPoint2.Zero)
            {
                plan.Steps.Add(SupplyStep(AutoCookStepKind.TakeBuffer, reagent, taken));
                amount -= taken;
                if (amount <= FixedPoint2.Zero)
                    return true;
            }
        }

        if (IsBaseReagent(comp, reagent))
        {
            plan.Steps.Add(SupplyStep(AutoCookStepKind.Synthesize, reagent, amount));
            return true;
        }

        if (!_reactionsByProduct.TryGetValue(reagent, out var reactions))
            return false;

        foreach (var reaction in reactions)
        {
            var produced = reaction.Products[reagent];
            if (produced <= 0 || !CanMix(comp, reaction))
                continue;

            var batches = GetBatches(amount, produced);
            var attempt = plan.Branch();

            if (!reaction.Reactants.All(pair => TryPlanReactant(comp, attempt, pair.Key, pair.Value, batches, depth + 1)))
                continue;

            AddConditionSteps(comp, reaction, attempt.Steps);
            attempt.Steps.Add(ReactStep(comp, reaction, reagent, batches));
            plan.Adopt(attempt);
            return true;
        }

        return false;
    }

    private bool TryPlanReactant(
        AutoCookerComponent comp,
        ReagentPlan plan,
        ProtoId<ReagentPrototype> reactant,
        ReactantPrototype data,
        int batches,
        int depth)
    {
        var amount = data.Amount * batches;

        if (data.Catalyst && plan.TryReserve(reactant, amount))
            return true;

        return TryPlanReagent(comp, plan, reactant, amount, depth);
    }

    private static int GetBatches(FixedPoint2 amount, FixedPoint2 produced)
    {
        return (int) MathF.Ceiling((amount / produced).Float());
    }

    private static AutoCookStep SupplyStep(AutoCookStepKind kind, ProtoId<ReagentPrototype> reagent, FixedPoint2 amount)
    {
        var step = new AutoCookStep(new AutoCookStepData(kind, reagent, 0f, TimeSpan.Zero));
        step.Produce[reagent] = amount;
        return step;
    }

    private static AutoCookStep ReactStep(AutoCookerComponent comp, ReactionPrototype reaction, string reagent, int batches)
    {
        var step = new AutoCookStep(new AutoCookStepData(AutoCookStepKind.React, reagent, 0f, comp.ReactionDuration));

        foreach (var (reactant, data) in reaction.Reactants)
        {
            if (!data.Catalyst)
                step.Consume[reactant] = data.Amount * batches;
        }

        foreach (var (product, quantity) in reaction.Products)
        {
            step.Produce[product] = quantity * batches;
        }

        return step;
    }

    /// <summary>
    /// Склеивает синтез и забор из буфера одного и того же реагента в один этап и считает их длительность.
    /// </summary>
    private static List<AutoCookStep> MergeSupplySteps(AutoCookerComponent comp, List<AutoCookStep> raw)
    {
        var result = new List<AutoCookStep>();
        var supplies = new Dictionary<(AutoCookStepKind, string), AutoCookStep>();

        foreach (var step in raw)
        {
            var kind = step.Data.Kind;
            if (kind is AutoCookStepKind.Synthesize or AutoCookStepKind.TakeBuffer)
            {
                var subject = step.Data.Subject;
                if (supplies.TryGetValue((kind, subject), out var existing))
                {
                    existing.Produce[subject] += step.Produce[subject];
                    continue;
                }

                supplies[(kind, subject)] = step;
            }

            result.Add(step);
        }

        foreach (var ((kind, subject), step) in supplies)
        {
            var amount = step.Produce[subject].Float();
            var duration = kind == AutoCookStepKind.Synthesize
                ? comp.SynthesisDuration + comp.SynthesisDurationPerUnit * amount
                : comp.BufferTakeDuration;

            step.Data = step.Data with { Value = amount, Duration = duration };
        }

        return result;
    }

    private static int CountReactions(List<AutoCookStep> steps)
    {
        return steps.Count(step => step.Data.Kind == AutoCookStepKind.React);
    }

    private static bool CanMix(AutoCookerComponent comp, ReactionPrototype reaction)
    {
        return reaction.MixingCategories == null
               || reaction.MixingCategories.All(comp.AllowedMixing.Contains);
    }

    private static void AddConditionSteps(AutoCookerComponent comp, ReactionPrototype reaction, List<AutoCookStep> steps)
    {
        foreach (var category in reaction.MixingCategories ?? [])
        {
            steps.Add(new AutoCookStep(new AutoCookStepData(AutoCookStepKind.Mixing, category, 0f, comp.ConditionDuration)));
        }

        if (reaction.MinimumTemperature > Atmospherics.T20C + TemperatureTolerance)
        {
            var heat = new AutoCookStepData(AutoCookStepKind.Heat, string.Empty, MathF.Round(reaction.MinimumTemperature), comp.ConditionDuration);
            steps.Add(new AutoCookStep(heat));
        }

        if (reaction.MaximumTemperature < Atmospherics.T20C - TemperatureTolerance)
        {
            var cool = new AutoCookStepData(AutoCookStepKind.Cool, string.Empty, MathF.Round(reaction.MaximumTemperature), comp.ConditionDuration);
            steps.Add(new AutoCookStep(cool));
        }
    }

    private static void ApplyStep(AutoCookJob job, AutoCookStep step)
    {
        foreach (var (id, amount) in step.Consume)
        {
            job.Stock[id] = FixedPoint2.Max(FixedPoint2.Zero, job.Stock.GetValueOrDefault(id) - amount);
        }

        foreach (var (id, amount) in step.Produce)
        {
            job.Stock[id] = job.Stock.GetValueOrDefault(id) + amount;
        }
    }

    private FixedPoint2 GetBeakerSpace(Entity<AutoCookerComponent> ent)
    {
        return _itemSlots.GetItemOrNull(ent, AutoCookerComponent.BeakerSlotId) is { } beaker
               && _solution.TryGetFitsInDispenser(beaker, out _, out var solution)
            ? solution.AvailableVolume
            : FixedPoint2.Zero;
    }

    /// <summary>
    /// Переливает результат в стакан. Возвращает false, если стакана нет или он полон.
    /// </summary>
    private bool TryOutputReagent(Entity<AutoCookerComponent> ent, AutoCookJob job)
    {
        if (_itemSlots.GetItemOrNull(ent, AutoCookerComponent.BeakerSlotId) is not { } beaker
            || !_solution.TryGetFitsInDispenser(beaker, out var soln, out var solution)
            || solution.AvailableVolume <= FixedPoint2.Zero)
            return false;

        ProtoId<ReagentPrototype> reagent = job.Recipe.Id;
        var amount = FixedPoint2.Min(job.TargetAmount, job.Stock.GetValueOrDefault(reagent));
        amount = FixedPoint2.Min(amount, solution.AvailableVolume);

        if (amount > FixedPoint2.Zero)
            _solution.TryAddReagent(soln.Value, reagent, amount);

        return true;
    }

    private bool TryGetBuffer(Entity<AutoCookerComponent> ent, [NotNullWhen(true)] out Entity<SolutionComponent>? buffer)
    {
        buffer = null;
        return ent.Comp.BufferSolution is { } name
               && _solution.TryGetSolution(ent.Owner, name, out buffer, out _);
    }

    private Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> GetBufferContents(Entity<AutoCookerComponent> ent)
    {
        var contents = new Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>();
        if (!TryGetBuffer(ent, out var buffer))
            return contents;

        foreach (var quantity in buffer.Value.Comp.Solution.Contents)
        {
            ProtoId<ReagentPrototype> id = quantity.Reagent.Prototype;
            contents[id] = contents.GetValueOrDefault(id) + quantity.Quantity;
        }

        return contents;
    }

    private bool TryDrainBuffer(Entity<AutoCookerComponent> ent, Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> amounts)
    {
        if (amounts.Count == 0)
            return true;

        if (!TryGetBuffer(ent, out var buffer))
            return false;

        foreach (var (id, amount) in amounts)
        {
            if (_solution.RemoveReagent(buffer.Value, id, amount) < amount)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Отмена задания синтеза возвращает забранные из буфера реагенты обратно.
    /// </summary>
    private void RefundBuffer(Entity<AutoCookerComponent> ent, AutoCookJob job)
    {
        if (job.Finished || job.ConsumedReagents.Count == 0 || !TryGetBuffer(ent, out var buffer))
            return;

        foreach (var (id, amount) in job.ConsumedReagents)
        {
            _solution.TryAddReagent(buffer.Value, id, amount);
        }
    }

    /// <summary>
    /// Сливает буфер в стакан, сколько поместится.
    /// </summary>
    private void FlushBuffer(Entity<AutoCookerComponent> ent)
    {
        if (!TryGetBuffer(ent, out var buffer)
            || _itemSlots.GetItemOrNull(ent, AutoCookerComponent.BeakerSlotId) is not { } beaker
            || !_solution.TryGetFitsInDispenser(beaker, out var output, out var outputSolution))
            return;

        var amount = FixedPoint2.Min(buffer.Value.Comp.Solution.Volume, outputSolution.AvailableVolume);
        if (amount <= FixedPoint2.Zero)
            return;

        var split = _solution.SplitSolution(buffer.Value, amount);
        _solution.TryAddSolution(output.Value, split);
    }

    /// <summary>
    /// Переливает содержимое стакана в буфер, сколько поместится.
    /// </summary>
    private void FillBuffer(Entity<AutoCookerComponent> ent)
    {
        if (!TryGetBuffer(ent, out var buffer)
            || _itemSlots.GetItemOrNull(ent, AutoCookerComponent.BeakerSlotId) is not { } beaker
            || !_solution.TryGetFitsInDispenser(beaker, out var input, out var inputSolution))
            return;

        var amount = FixedPoint2.Min(inputSolution.Volume, buffer.Value.Comp.Solution.AvailableVolume);
        if (amount <= FixedPoint2.Zero)
            return;

        var split = _solution.SplitSolution(input.Value, amount);
        _solution.TryAddSolution(buffer.Value, split);
    }
}
