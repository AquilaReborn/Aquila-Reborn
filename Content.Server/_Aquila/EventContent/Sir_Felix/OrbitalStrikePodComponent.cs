using Content.Shared._Aquila.EventContent.Sir_Felix;

namespace Content.Server._Aquila.EventContent.Sir_Felix;

/// <summary>
/// Капсула орбитального удара, взрывающаяся в указанное время.
/// </summary>
[RegisterComponent, Access(typeof(OrbitalStrikeSystem))]
public sealed partial class OrbitalStrikePodComponent : Component
{
    [ViewVariables]
    public OrbitalExplosionMode Mode = new() { Name = string.Empty };

    [ViewVariables]
    public TimeSpan ExplodeAt;
}
