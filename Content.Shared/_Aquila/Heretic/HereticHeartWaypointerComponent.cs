using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Aquila.Heretic;

/// <summary>
/// Стрелка живого сердца, которая ведёт еретика к выбранной жертве.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedHereticHeartWaypointerSystem))]
public sealed partial class HereticHeartWaypointerComponent : Component
{
    /// <summary>
    /// Сколько времени стрелка видна после активации сердца.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Момент, когда стрелка гаснет.
    /// </summary>
    [DataField, AutoPausedField, AutoNetworkedField]
    public TimeSpan ExpiresAt;

    /// <summary>
    /// Жертва, на которую указывает стрелка.
    /// </summary>
    [AutoNetworkedField]
    public NetEntity? Target;

    /// <summary>
    /// Мировая позиция жертвы.
    /// </summary>
    [AutoNetworkedField]
    public Vector2 TargetPosition;

    /// <summary>
    /// Расстояние от еретика до жертвы.
    /// </summary>
    [AutoNetworkedField]
    public float Distance;

    /// <summary>
    /// Находится ли жертва на той же карте, что и еретик.
    /// </summary>
    [AutoNetworkedField]
    public bool SameMap;

    /// <summary>
    /// Дальность, начиная с которой градация считается максимальной.
    /// </summary>
    [DataField]
    public float MaxRange = 30f;

    /// <summary>
    /// На сколько тайлов от еретика висит стрелка.
    /// </summary>
    [DataField]
    public float Offset = 1.2f;

    /// <summary>
    /// Момент следующего обновления позиции жертвы.
    /// </summary>
    public TimeSpan NextUpdate;
}
