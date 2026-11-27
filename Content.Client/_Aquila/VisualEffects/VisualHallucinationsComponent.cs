using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client._Aquila.VisualEffects;

/// <summary>
/// Клиентское состояние единственного текущего галлюцинаторного образа наблюдателя.
/// </summary>
[RegisterComponent, Access(typeof(VisualHallucinationsSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class VisualHallucinationsComponent : Component
{
    public EntityUid? Source;
    public SpriteSpecifier? Image;
    public TimeSpan NextImage;
    public TimeSpan StartedAt;
    public TimeSpan EndsAt;
    public Vector2 Position;
    public MapId MapId;
}
