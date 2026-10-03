using Content.Shared._Aquila.EventContent.Sir_Felix;
using Robust.Shared.Map;

namespace Content.Server._Aquila.EventContent.Sir_Felix;

/// <summary>
/// Идущий сейчас орбитальный удар: пока компонент есть на предмете, он по таймеру сбрасывает капсулы.
/// </summary>
[RegisterComponent, Access(typeof(OrbitalStrikeSystem))]
public sealed partial class OrbitalStrikeActiveComponent : Component
{
    [ViewVariables]
    public MapCoordinates Center;

    [ViewVariables]
    public float Radius;

    [ViewVariables]
    public OrbitalExplosionMode Mode = new() { Name = string.Empty };

    [ViewVariables]
    public int Remaining;

    [ViewVariables]
    public TimeSpan NextSpawn;
}
