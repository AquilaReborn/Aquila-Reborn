namespace Content.Shared._Aquila.Cyberware.Components;

/// <summary>
/// Имплант, орган или кибер-конечность, которая нагружает организм носителя.
/// </summary>
[RegisterComponent]
public sealed partial class CyberwareComponent : Component
{
    /// <summary>
    /// Кибернагрузка, которую этот имплант даёт телу, пока установлен.
    /// </summary>
    [DataField]
    public float Load = 5f;
}
