using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Heretic.Events;

/// <summary>
/// Сообщает клиенту, что у еретика сработало зрение сдвига реальности, и сколько времени оно будет держаться:
/// клиент показывает те же эффекты Мансуса, что и у жертвы испытания, и плавно гасит их к концу зрения.
/// </summary>
[Serializable, NetSerializable]
public sealed class RealityShiftVisionStartedEvent : EntityEventArgs
{
    /// <summary>
    /// Сколько времени зрение держится с момента срабатывания.
    /// </summary>
    public TimeSpan Duration;
}
