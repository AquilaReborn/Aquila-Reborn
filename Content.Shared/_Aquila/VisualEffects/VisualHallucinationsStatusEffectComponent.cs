using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._Aquila.VisualEffects;

/// <summary>
/// Настройки кратких локальных образов, сопровождающих статус-эффект.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class VisualHallucinationsStatusEffectComponent : Component
{
    /// <summary>
    /// Изображения, из которых случайно выбирается очередной образ.
    /// </summary>
    [DataField]
    public List<SpriteSpecifier> Images = new();

    /// <summary>
    /// Минимальная пауза между образами.
    /// </summary>
    [DataField]
    public TimeSpan MinInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Максимальная пауза между образами.
    /// </summary>
    [DataField]
    public TimeSpan MaxInterval = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Время появления и плавного исчезновения одного образа.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Минимальное расстояние от наблюдателя до образа.
    /// </summary>
    [DataField]
    public float MinDistance = 2;

    /// <summary>
    /// Максимальное расстояние от наблюдателя до образа.
    /// </summary>
    [DataField]
    public float MaxDistance = 4;

    /// <summary>
    /// Размер изображения в мировых единицах.
    /// </summary>
    [DataField]
    public float ImageSize = 1.5f;

    /// <summary>
    /// Максимальная непрозрачность образа.
    /// </summary>
    [DataField]
    public float Opacity = 0.7f;
}
