using Robust.Shared.GameStates;

namespace Content.Shared._Aquila.Ladder.Components;

/// <summary>
/// Метка на существе, которое только что прибыло по лестнице. Не даёт лестнице-выходу сразу отправить его обратно.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedLadderSystem))]
public sealed partial class LadderArrivalComponent : Component
{
    /// <summary>
    /// Лестница, на которой появилось существо.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? Ladder;

    /// <summary>
    /// До какого момента эта лестница игнорирует существо.
    /// </summary>
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan IgnoreUntil;
}
