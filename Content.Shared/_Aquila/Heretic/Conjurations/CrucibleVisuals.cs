using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Heretic.Conjurations;

/// <summary>
/// Ключи визуальных данных Зубастого тигля.
/// </summary>
[Serializable, NetSerializable]
public enum CrucibleVisuals : byte
{
    /// <summary>
    /// Состояние слоя: тигель полон или пуст.
    /// </summary>
    State,
}
