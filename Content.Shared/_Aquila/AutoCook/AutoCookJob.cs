using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.AutoCook;

/// <summary>
/// Один этап задания. <see cref="Consume"/> и <see cref="Produce"/> нужны только для синтеза реагентов.
/// </summary>
public sealed class AutoCookStep(AutoCookStepData data)
{
    public AutoCookStepData Data = data;
    public readonly Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Consume = new();
    public readonly Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Produce = new();
}

public sealed class AutoCookJob(AutoCookRecipeId recipe, string result, List<AutoCookStep> steps)
{
    public readonly AutoCookRecipeId Recipe = recipe;

    /// <summary>
    /// Прототип результата: реагент или сущность, по нему клиент показывает название.
    /// </summary>
    public readonly string Result = result;

    public readonly List<AutoCookStep> Steps = steps;
    public int StepIndex;
    public TimeSpan StepStart;

    /// <summary>
    /// Сколько прошло от начала этапа к моменту, когда пропало питание. Не null, пока задание на паузе.
    /// </summary>
    public TimeSpan? PausedElapsed;

    public bool WaitingOutput;
    public float IdleLoad;

    /// <summary>
    /// Промежуточные реагенты синтеза, которые машина держит внутри себя.
    /// </summary>
    public readonly Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Stock = new();

    public FixedPoint2 TargetAmount;
    public int ResultCount = 1;
    public Dictionary<EntProtoId, int> ConsumedSolids = new();
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> ConsumedReagents = new();

    public bool Finished => StepIndex >= Steps.Count;
}

public sealed record AutoCookOrder(AutoCookRecipeId Recipe, int Amount);
