namespace Content.Shared._Aquila.AutoCook;

public enum AutoCookStepKind : byte
{
    Synthesize,
    React,
    Prepare,
    Cook,
    Condition,
}

public sealed class AutoCookStep
{
    public AutoCookStepKind Kind;
    public string Subject = string.Empty;
    public string Text = string.Empty;
    public float Duration;
    public Dictionary<string, float> Consume = new();
    public Dictionary<string, float> Produce = new();
}

public sealed class AutoCookJob
{
    public string RecipeName = string.Empty;
    public List<AutoCookStep> Steps = new();
    public int StepIndex;
    public TimeSpan StepStart;
    public TimeSpan PausedElapsed;
    public bool Paused;
    public bool WaitingOutput;
    public float IdleLoad;
    public Dictionary<string, float> Stock = new();
    public string? TargetReagent;
    public float TargetAmount;
    public string? ResultEntity;
    public int ResultCount = 1;
    public Dictionary<string, int> ConsumedSolids = new();
    public Dictionary<string, float> ConsumedReagents = new();
}

public sealed record AutoCookOrder(string RecipeId, int Amount);
