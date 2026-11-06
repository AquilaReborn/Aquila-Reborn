using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Aquila.Ladder.Components;

/// <summary>
/// Лестница или люк: встав на неё и постояв рядом, существо переносится к другой лестнице с тем же ключом связи.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedLadderSystem))]
public sealed partial class LadderComponent : Component
{
    /// <summary>
    /// Ключ связи. Лестницы с одинаковым ключом ведут друг к другу, даже на разных картах и гридах.
    /// Если подходящих лестниц несколько, выход выбирается случайно. Пустой ключ - лестница никуда не ведёт.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? LinkId;

    /// <summary>
    /// Работает ли лестница. Выключенная никого не переносит и не служит выходом.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Сколько нужно простоять у лестницы, чтобы перебраться. Ноль - мгновенный переход без полоски и попапа.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// На каком расстоянии от лестницы нужно оставаться, пока идёт перенос.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 1.5f;

    /// <summary>
    /// Сколько после прибытия лестница-выход не реагирует на того, кто на ней появился.
    /// </summary>
    [DataField]
    public TimeSpan ArrivalGrace = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Переносить ли вместе с существом того, кого оно тащит.
    /// </summary>
    [DataField]
    public bool CarryPulled = true;

    /// <summary>
    /// Звук на обоих концах лестницы при переносе.
    /// </summary>
    [DataField]
    public SoundSpecifier? ClimbSound = new SoundCollectionSpecifier("FootstepCatwalk");
}
