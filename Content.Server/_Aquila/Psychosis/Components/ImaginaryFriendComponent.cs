using Robust.Shared.Player;

namespace Content.Server._Aquila.Psychosis.Components;

/// <summary>
/// Воображаемый друг, которого видит и слышит только его хозяин.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(ImaginaryFriendSystem))]
public sealed partial class ImaginaryFriendComponent : Component
{
    /// <summary>
    /// Тот, кому мерещится друг. Пусто, пока энграмма заперта в чипе.
    /// </summary>
    [ViewVariables]
    public EntityUid? Owner;

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

    /// <summary>
    /// Сообщение, которое видит хозяин, когда друг появляется рядом.
    /// </summary>
    [DataField]
    public LocId AppearedMessage = "imaginary-friend-appeared";

    /// <summary>
    /// Глушит ли друга галоперидол и лечит ли его нейролептин. Энграммам лекарства не страшны.
    /// </summary>
    [DataField]
    public bool AffectedByMedicine = true;

    /// <summary>
    /// До какого момента роль друга должен занять призрак, иначе друг исчезает сам.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan ClaimDeadline;
}
