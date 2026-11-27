using Content.Shared._Aquila.Heretic.Moon.Systems;

namespace Content.Shared._Aquila.Heretic.Moon.Components;

/// <summary>
/// Носитель пышного облачения пути Луны: весь входящий урон переводится в урон мозгу.
/// </summary>
[RegisterComponent, Access(typeof(SharedMoonRegaliaSystem))]
public sealed partial class MoonRegaliaWearerComponent : Component
{
    /// <summary>
    /// Множитель перевода урона в мозговой.
    /// </summary>
    [DataField]
    public float BrainDamageMultiplier = 1.15f;

    /// <summary>
    /// True, если пацифизм выдало именно это облачение, а не другой источник.
    /// </summary>
    [DataField]
    public bool GrantedPacified;
}
