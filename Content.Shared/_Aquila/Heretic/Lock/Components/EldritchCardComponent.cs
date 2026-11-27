using Content.Shared.Teleportation.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Heretic.Lock.Components;

/// <summary>
/// Карта Хранителя ключей: связывает два портала в пару, хранит ссылки на неё и работает как ID-карта.
/// </summary>
[RegisterComponent]
public sealed partial class EldritchCardComponent : Component
{
    /// <summary>
    /// Прототип портала, который создаёт карта.
    /// </summary>
    [DataField]
    public EntProtoId<PortalComponent> PortalPrototype = "HereticLockPortal";

    /// <summary>
    /// Первый портал текущей пары.
    /// </summary>
    public EntityUid? FirstPortal;

    /// <summary>
    /// Второй портал текущей пары.
    /// </summary>
    public EntityUid? SecondPortal;

    /// <summary>
    /// Место, отмеченное картой для второй половины пары.
    /// </summary>
    public EntityCoordinates? PendingCoords;
}
