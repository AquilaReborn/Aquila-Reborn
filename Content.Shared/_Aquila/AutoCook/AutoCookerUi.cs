using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.AutoCook;

public static class AutoCookIds
{
    public const string Reagent = "reagent:";
    public const string Meal = "meal:";
    public const string Mix = "mix:";
    public const string Make = "make:";
}

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
public enum AutoCookStepStatus : byte
{
    Pending,
    Active,
    Done,
}

[Serializable, NetSerializable]
public sealed record AutoCookIngredient(string Label, string Amount, bool Ok);

[Serializable, NetSerializable]
public sealed record AutoCookRecipeEntry(
    string Id,
    string Name,
    string Group,
    bool Available,
    List<AutoCookIngredient> Ingredients,
    List<string> Steps);

[Serializable, NetSerializable]
public sealed record AutoCookStepInfo(string Text, float Duration, AutoCookStepStatus Status);

[Serializable, NetSerializable]
public sealed record AutoCookJobInfo(
    string RecipeName,
    List<AutoCookStepInfo> Steps,
    int ActiveIndex,
    TimeSpan StepStart,
    TimeSpan PausedElapsed,
    bool Paused,
    bool WaitingOutput);

[Serializable, NetSerializable]
public sealed record AutoCookOutputInfo(string Name, FixedPoint2 Volume, FixedPoint2 MaxVolume, Color Color);

[Serializable, NetSerializable]
public sealed record AutoCookStockEntry(string Name, string Amount);

[Serializable, NetSerializable]
public sealed record AutoCookQueueEntry(string Name, int Amount);

[Serializable, NetSerializable]
public sealed class AutoCookerBoundUserInterfaceState(
    AutoCookKind kind,
    bool powered,
    List<AutoCookRecipeEntry> recipes,
    AutoCookJobInfo? job,
    List<AutoCookQueueEntry> queue,
    int maxQueue,
    AutoCookOutputInfo? output,
    List<AutoCookStockEntry> stock) : BoundUserInterfaceState
{
    public readonly AutoCookKind Kind = kind;
    public readonly bool Powered = powered;
    public readonly List<AutoCookRecipeEntry> Recipes = recipes;
    public readonly AutoCookJobInfo? Job = job;
    public readonly List<AutoCookQueueEntry> Queue = queue;
    public readonly int MaxQueue = maxQueue;
    public readonly AutoCookOutputInfo? Output = output;
    public readonly List<AutoCookStockEntry> Stock = stock;
}

[Serializable, NetSerializable]
public sealed class AutoCookerStartMessage(string recipeId, int amount) : BoundUserInterfaceMessage
{
    public readonly string RecipeId = recipeId;
    public readonly int Amount = amount;
}

[Serializable, NetSerializable]
public sealed class AutoCookerCancelMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AutoCookerRemoveQueuedMessage(int index) : BoundUserInterfaceMessage
{
    public readonly int Index = index;
}
