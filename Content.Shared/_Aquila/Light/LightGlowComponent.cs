using Robust.Shared.Utility;

namespace Content.Shared._Aquila.Light;

/// <summary>
/// Декоративное свечение светильника: текстура, которая добавляется поверх мира там, куда доходит его PointLight.
/// </summary>
[RegisterComponent]
public sealed partial class LightGlowComponent : Component
{
    /// <summary>
    /// Текстура свечения. Источник в центре, свет уходит вниз.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Texture = default!;

    /// <summary>
    /// Размер текстуры в тайлах.
    /// </summary>
    [DataField]
    public float Size = 7f;

    /// <summary>
    /// Насколько центр свечения сдвинут от светильника назад, к стене, в тайлах.
    /// </summary>
    [DataField]
    public float Offset;

    /// <summary>
    /// Сила свечения при energy у PointLight, равном 1.
    /// </summary>
    [DataField]
    public float Opacity = 1f;
}
