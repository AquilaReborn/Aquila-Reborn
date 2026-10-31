namespace Content.Server._Aquila.StationEvents;

/// <summary>
/// Психическая аномалия: у нескольких членов экипажа появляется раздвоение личности или воображаемый друг.
/// </summary>
[RegisterComponent, Access(typeof(PsychicAnomalyRule))]
public sealed partial class PsychicAnomalyRuleComponent : Component
{
    /// <summary>
    /// Минимум пострадавших.
    /// </summary>
    [DataField]
    public int MinTargets = 1;

    /// <summary>
    /// Максимум пострадавших.
    /// </summary>
    [DataField]
    public int MaxTargets = 2;

    /// <summary>
    /// Шанс, что пострадавший получит воображаемого друга, а не раздвоение личности.
    /// </summary>
    [DataField]
    public float FriendChance = 0.5f;
}
