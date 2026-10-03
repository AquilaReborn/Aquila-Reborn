namespace Content.Shared._Aquila.EventContent.Sir_Felix;

/// <summary>
/// Режим взрыва орбитального удара.
/// </summary>
[DataDefinition]
public sealed partial class OrbitalExplosionMode
{
    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public float Intensity = 120f;

    [DataField]
    public float Slope = 1f;

    [DataField]
    public float MaxTileIntensity = 12f;
}
