using Content.Server._Aquila.Heretic.Moon.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Aquila.Heretic.Moon.Components;

/// <summary>
/// Мирская цель, попавшая под лунную ауру: держит визуальный оверлей безумия, пока аура обновляет эффект.
/// </summary>
[RegisterComponent, Access(typeof(MoonAscensionSystem)), AutoGenerateComponentPause]
public sealed partial class MoonInsanityAuraComponent : Component
{
    /// <summary>
    /// Время, до которого оверлей считается обновлённым аурой.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// Сущность визуального оверлея, привязанная к цели.
    /// </summary>
    public EntityUid? Overlay;
}
