using Robust.Shared.Player;

namespace Content.Server._Aquila.Psychosis.Components;

/// <summary>
/// Воображаемый друг, которого видит и слышит только его хозяин.
/// </summary>
[RegisterComponent]
public sealed partial class ImaginaryFriendComponent : Component
{
    /// <summary>
    /// Тот, кому мерещится друг.
    /// </summary>
    [ViewVariables]
    public EntityUid Owner;

    /// <summary>
    /// Сессия, которой сейчас принудительно отправляется друг.
    /// </summary>
    [ViewVariables]
    public ICommonSession? ShownTo;

    /// <summary>
    /// Друг заглушён лекарством: хозяин его не видит и не слышит.
    /// </summary>
    [ViewVariables]
    public bool Suppressed;

    /// <summary>
    /// Насколько далеко друг может отойти от хозяина, в тайлах.
    /// </summary>
    [DataField]
    public float MaxDistance = 8f;
}
