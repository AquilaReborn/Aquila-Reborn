using Content.Server._Aquila.Heretic.Conjurations.Systems;
using Robust.Shared.Map;

namespace Content.Server._Aquila.Heretic.Conjurations.Components;

/// <summary>
/// Носитель эффекта души тигля: запоминает точку возврата и остаётся в фазовом сдвиге, пока эффект активен.
/// </summary>
[RegisterComponent, Access(typeof(CrucibleSoulSystem))]
public sealed partial class CrucibleSoulComponent : Component
{
    /// <summary>
    /// Координаты, в которые носитель телепортируется после окончания эффекта.
    /// </summary>
    [ViewVariables]
    public MapCoordinates ReturnCoordinates;

    /// <summary>
    /// Длительность кулдауна, который выдаётся после окончания эффекта.
    /// </summary>
    [DataField]
    public TimeSpan CooldownDuration = TimeSpan.FromMinutes(2);
}
