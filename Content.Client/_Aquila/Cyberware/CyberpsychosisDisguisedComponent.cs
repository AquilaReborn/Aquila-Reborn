namespace Content.Client._Aquila.Cyberware;

/// <summary>
/// Человек, которого больной киберпсихозом видит в чужом враждебном облике. Существует только на клиенте больного.
/// Настоящий спрайт не прячется, а становится полностью прозрачным через пост-шейдер, чтобы по нему можно было кликать, бить и хватать.
/// </summary>
[RegisterComponent, Access(typeof(CyberpsychosisSystem))]
public sealed partial class CyberpsychosisDisguisedComponent : Component
{
    /// <summary>
    /// Сущность с враждебным обликом, прикреплённая к человеку.
    /// </summary>
    [ViewVariables]
    public EntityUid? Disguise;
}
