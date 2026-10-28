using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Aquila.Audio;

/// <summary>
/// Периодически проверяет, находится ли существо в вакууме, и выставляет ему <see cref="Content.Shared._Aquila.Audio.InVacuumComponent"/>.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(VacuumExposureSystem))]
public sealed partial class VacuumExposureComponent : Component
{
    /// <summary>
    /// Когда в следующий раз проверить давление вокруг существа.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextCheck;
}
