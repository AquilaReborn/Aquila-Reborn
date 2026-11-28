using Content.Client._Aquila.Heretic.Moon.Systems;

namespace Content.Client._Aquila.Heretic.Moon.Components;

/// <summary>
/// Хранит локальный момент появления зеркального раскола.
/// </summary>
[RegisterComponent, Access(typeof(MoonParadeFractureSystem)), AutoGenerateComponentPause]
public sealed partial class MoonParadeFractureVisualsComponent : Component
{
    /// <summary>
    /// Когда клиент начал показывать этот статус.
    /// </summary>
    [AutoPausedField]
    public TimeSpan StartedAt;
}
