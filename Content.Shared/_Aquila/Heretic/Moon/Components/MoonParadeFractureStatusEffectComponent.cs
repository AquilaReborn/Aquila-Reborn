using Robust.Shared.GameStates;

namespace Content.Shared._Aquila.Heretic.Moon.Components;

/// <summary>
/// Включает зеркальный раскол сцены для игрока, на которого наложен статус.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class MoonParadeFractureStatusEffectComponent : Component
{
    /// <summary>
    /// Время нарастания искажения после получения статуса клиентом.
    /// </summary>
    [DataField]
    public TimeSpan FadeIn = TimeSpan.FromSeconds(0.09);

    /// <summary>
    /// Время затухания искажения перед окончанием статуса.
    /// </summary>
    [DataField]
    public TimeSpan FadeOut = TimeSpan.FromSeconds(0.3);
}
