using Robust.Shared.Map;

namespace Content.Server._Aquila.Heretic;

/// <summary>
/// Маркер, что после возврата из Мансуса жертве нужно принудительно отправить окружение.
/// </summary>
[RegisterComponent, Access(typeof(MansusRealmSystem))]
public sealed partial class MansusReturnVisionComponent : Component
{
    /// <summary>
    /// Место ритуала, вокруг которого тоже принудительно отправляются сущности, может быть null.
    /// </summary>
    [ViewVariables]
    public EntityCoordinates? Anchor;

    /// <summary>
    /// Радиус, в котором принудительно отправляются сущности вокруг жертвы и места ритуала.
    /// </summary>
    [DataField]
    public float Radius = 12f;

    /// <summary>
    /// Время, которое жертва должна непрерывно провести рядом с местом ритуала, чтобы принудительная отправка прекратилась.
    /// </summary>
    [DataField]
    public TimeSpan ClearDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Время, после которого принудительная отправка прекращается в любом случае, даже если жертва так и не подошла к месту ритуала.
    /// </summary>
    [DataField]
    public TimeSpan ExpireAfter = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Момент, с которого началось принудительное отправление окружения.
    /// </summary>
    [ViewVariables]
    public TimeSpan StartedAt;

    /// <summary>
    /// Момент, с которого жертва непрерывно находится в радиусе места ритуала, null пока не подошла.
    /// </summary>
    [ViewVariables]
    public TimeSpan? InRangeSince;
}
