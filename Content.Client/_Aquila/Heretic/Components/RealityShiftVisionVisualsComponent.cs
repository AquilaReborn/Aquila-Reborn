using Content.Client._Aquila.Heretic.Systems;

namespace Content.Client._Aquila.Heretic.Components;

/// <summary>
/// Таймеры визуального эффекта зрения сдвига реальности у местного игрока. Существует только на клиенте.
/// </summary>
[RegisterComponent, Access(typeof(MansusRealmOverlaySystem))]
public sealed partial class RealityShiftVisionVisualsComponent : Component
{
    /// <summary>
    /// Момент, когда включился визуальный эффект.
    /// </summary>
    [ViewVariables]
    public TimeSpan StartTime;

    /// <summary>
    /// Момент, когда заканчивается само зрение: с него эффект начинает плавно гаснуть.
    /// </summary>
    [ViewVariables]
    public TimeSpan EndTime;
}
