using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.AutoCook;

[Serializable, NetSerializable]
public enum AutoCookKind : byte
{
    Bar,
    Kitchen,
    Chem,
}

[Serializable, NetSerializable]
public enum AutoCookerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum AutoCookerVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum AutoCookerVisualState : byte
{
    Off,
    Normal,
    Cooking,
    Done,
}

[Serializable, NetSerializable]
public enum AutoCookRecipeKind : byte
{
    /// <summary>
    /// Реагент, <see cref="AutoCookRecipeId.Id"/> - прототип реагента.
    /// </summary>
    Reagent,

    /// <summary>
    /// Блюдо, <see cref="AutoCookRecipeId.Id"/> - рецепт микроволновки.
    /// </summary>
    Meal,

    /// <summary>
    /// Замес теста реакцией, <see cref="AutoCookRecipeId.Id"/> - прототип реакции.
    /// </summary>
    Mix,

    /// <summary>
    /// Обработка инструментом, <see cref="AutoCookRecipeId.Id"/> - прототип результата.
    /// </summary>
    Make,
}

[Serializable, NetSerializable]
public readonly record struct AutoCookRecipeId(AutoCookRecipeKind Kind, string Id);

[Serializable, NetSerializable]
public enum AutoCookStepKind : byte
{
    Synthesize,
    TakeBuffer,
    React,
    Mixing,
    Heat,
    Cool,
    PrepareIngredients,
    Cook,
    Mix,
    Slice,
    Process,
}

[Serializable, NetSerializable]
public enum AutoCookStepStatus : byte
{
    Pending,
    Active,
    Done,
}

/// <summary>
/// Описание этапа. <see cref="Subject"/> - прототип реагента, сущности или категории смешивания,
/// <see cref="Value"/> - количество или температура.
/// </summary>
[Serializable, NetSerializable]
public sealed record AutoCookStepData(AutoCookStepKind Kind, string Subject, float Value, TimeSpan Duration);

/// <summary>
/// <see cref="Have"/> равен null, если наличие не проверяется, например для синтеза.
/// </summary>
[Serializable, NetSerializable]
public sealed record AutoCookIngredient(string Id, bool Reagent, FixedPoint2? Have, FixedPoint2 Need, bool Ok);

/// <summary>
/// <see cref="Result"/> - прототип результата для названия, <see cref="Group"/> - id локализации категории.
/// </summary>
[Serializable, NetSerializable]
public sealed record AutoCookRecipeEntry(
    AutoCookRecipeId Id,
    string Result,
    string Group,
    bool Available,
    List<AutoCookIngredient> Ingredients,
    List<AutoCookStepData> Steps);

[Serializable, NetSerializable]
public sealed record AutoCookJobInfo(
    AutoCookRecipeKind Kind,
    string Result,
    List<AutoCookStepData> Steps,
    int ActiveIndex,
    TimeSpan StepStart,
    TimeSpan? PausedElapsed,
    bool WaitingOutput);

[Serializable, NetSerializable]
public sealed record AutoCookOutputInfo(string Name, FixedPoint2 Volume, FixedPoint2 MaxVolume, Color Color);

[Serializable, NetSerializable]
public sealed record AutoCookStockEntry(string Id, bool Reagent, FixedPoint2 Amount);

[Serializable, NetSerializable]
public sealed record AutoCookQueueEntry(AutoCookRecipeKind Kind, string Result, int Amount);

[Serializable, NetSerializable]
public sealed class AutoCookerBoundUserInterfaceState(
    AutoCookKind kind,
    bool powered,
    List<AutoCookRecipeEntry> recipes,
    AutoCookJobInfo? job,
    List<AutoCookQueueEntry> queue,
    int maxQueue,
    AutoCookOutputInfo? output,
    List<AutoCookStockEntry> stock,
    bool hasBuffer) : BoundUserInterfaceState
{
    public readonly AutoCookKind Kind = kind;
    public readonly bool Powered = powered;
    public readonly List<AutoCookRecipeEntry> Recipes = recipes;
    public readonly AutoCookJobInfo? Job = job;
    public readonly List<AutoCookQueueEntry> Queue = queue;
    public readonly int MaxQueue = maxQueue;
    public readonly AutoCookOutputInfo? Output = output;
    public readonly List<AutoCookStockEntry> Stock = stock;

    /// <summary>
    /// У синтезатора есть буфер, <see cref="Stock"/> показывает его содержимое.
    /// </summary>
    public readonly bool HasBuffer = hasBuffer;
}

[Serializable, NetSerializable]
public sealed class AutoCookerStartMessage(AutoCookRecipeId recipe, int amount) : BoundUserInterfaceMessage
{
    public readonly AutoCookRecipeId Recipe = recipe;
    public readonly int Amount = amount;
}

[Serializable, NetSerializable]
public sealed class AutoCookerCancelMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AutoCookerRemoveQueuedMessage(int index) : BoundUserInterfaceMessage
{
    public readonly int Index = index;
}

[Serializable, NetSerializable]
public sealed class AutoCookerFlushBufferMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AutoCookerFillBufferMessage : BoundUserInterfaceMessage;
