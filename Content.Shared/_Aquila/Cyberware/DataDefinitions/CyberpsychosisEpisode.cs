using Content.Shared.EntityEffects;

namespace Content.Shared._Aquila.Cyberware.DataDefinitions;

/// <summary>
/// Один приступ киберпсихоза: набор эффектов, которые срабатывают вместе.
/// </summary>
[DataDefinition]
public sealed partial class CyberpsychosisEpisode
{
    /// <summary>
    /// Вес приступа при случайном выборе среди приступов стадии.
    /// </summary>
    [DataField]
    public float Weight = 1f;

    /// <summary>
    /// Эффекты, которые срабатывают во время приступа.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;
}
