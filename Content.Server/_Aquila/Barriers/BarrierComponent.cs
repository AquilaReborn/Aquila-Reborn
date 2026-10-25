using System.Numerics;
using Robust.Shared.Map;

namespace Content.Server.Aquila.Barriers;

[RegisterComponent]
public sealed partial class BarrierComponent : Component
{
    [DataField]
    public float LookupRadius = 0.6f;

    [DataField]
    public int BarrierLength = 3;

    [DataField]
    public float StepDistance = 1f;
    [DataField]
    public TimeSpan SpawnDelayPerStep = TimeSpan.FromSeconds(0.15f);

    [ViewVariables]
    public List<(TimeSpan Time, MapCoordinates Coords)> PendingSegments = new();
}
