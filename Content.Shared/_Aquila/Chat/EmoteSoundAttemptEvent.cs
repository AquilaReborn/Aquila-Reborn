namespace Content.Shared._Aquila.Chat;

/// <summary>
/// Вызывается на сущности перед воспроизведением звука эмоции. Отмена убирает звук, но эмоция остаётся в чате.
/// </summary>
[ByRefEvent]
public record struct EmoteSoundAttemptEvent(bool Cancelled = false);
