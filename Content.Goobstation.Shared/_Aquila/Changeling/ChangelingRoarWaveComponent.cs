using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared._Aquila.Changeling;

/// <summary>
/// Волна искажения экрана от рёва истинной формы генокрада. Только визуальный эффект.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingRoarWaveComponent : Component
{
    /// <summary>
    /// Скорость расхождения волн от центра.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WaveSpeed = 15.3f;

    /// <summary>
    /// Ширина и сила искажения волны.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WaveStrength = 1.0f;

    /// <summary>
    /// Масштаб эффекта: чем меньше число, тем больше охват.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DownScale = 1.5f;

    /// <summary>
    /// За сколько секунд волна полностью исчезает.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FadeTime = 3.0f;
}
