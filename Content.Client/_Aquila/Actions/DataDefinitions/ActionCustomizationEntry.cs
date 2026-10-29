using System.Numerics;
using Robust.Client.Input;

namespace Content.Client._Aquila.Actions.DataDefinitions;

[DataDefinition]
public sealed partial class ActionCustomizationEntry
{
    /// <summary>
    /// Клавиша собственного бинда действия, null - бинд не назначен.
    /// </summary>
    [DataField]
    public Keyboard.Key? Key;

    /// <summary>
    /// Нужно ли держать Shift для срабатывания бинда.
    /// </summary>
    [DataField]
    public bool Shift;

    /// <summary>
    /// Нужно ли держать Ctrl для срабатывания бинда.
    /// </summary>
    [DataField]
    public bool Control;

    /// <summary>
    /// Нужно ли держать Alt для срабатывания бинда.
    /// </summary>
    [DataField]
    public bool Alt;

    /// <summary>
    /// Положение вынесенной на экран кнопки в долях от размера экрана, null - кнопка не вынесена.
    /// </summary>
    [DataField]
    public Vector2? Position;
}
