using Robust.Shared.GameStates;

namespace Content.Shared._Aquila.Cyberware.Components;

/// <summary>
/// Что глубокое сканирование узнало об имплантах этого существа.
/// Опознанные импланты показываются в отчёте сканера и могут быть извлечены операцией.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ImplantsIdentifiedComponent : Component
{
    /// <summary>
    /// Импланты, которые удалось опознать.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Identified = new();

    /// <summary>
    /// Когда этого человека снова можно глубоко сканировать.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextDeepScan;
}
