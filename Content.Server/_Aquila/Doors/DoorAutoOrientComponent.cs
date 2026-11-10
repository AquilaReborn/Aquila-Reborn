namespace Content.Server._Aquila.Doors;

/// <summary>
/// Дверь (или пластиковые шторки) с направленным спрайтом, которую при MapInit разворачивает вдоль стены:
/// в горизонтальной стене она смотрит на юг, в вертикальной на восток.
/// Нужна, потому что на картах апстрима все двери стоят с поворотом 0.
/// </summary>
[RegisterComponent, Access(typeof(DoorAutoOrientSystem))]
public sealed partial class DoorAutoOrientComponent : Component
{
    /// <summary>
    /// Включён ли автоповорот. Потомки с однонаправленным спрайтом его выключают.
    /// </summary>
    [DataField]
    public bool Enabled = true;
}
