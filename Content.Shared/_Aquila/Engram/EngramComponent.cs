using Robust.Shared.GameStates;

namespace Content.Shared._Aquila.Engram;

/// <summary>
/// Цифровой слепок личности. На клиенте рисуется голограммой с помехами.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EngramComponent : Component
{
    /// <summary>
    /// Контекст ввода, который получает игрок энграммы. Компонент ввода не сетевой, поэтому клиент добавляет его сам.
    /// </summary>
    [DataField]
    public string InputContext = "ghost";
}
