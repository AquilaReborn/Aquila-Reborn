namespace Content.Server._Aquila.Psychosis.Components;

/// <summary>
/// Сущность внутри тела, в которой находится личность, не управляющая телом.
/// </summary>
[RegisterComponent]
public sealed partial class SplitPersonalityPassengerComponent : Component
{
    /// <summary>
    /// Тело, внутри которого находится личность.
    /// </summary>
    [ViewVariables]
    public EntityUid Host;
}
