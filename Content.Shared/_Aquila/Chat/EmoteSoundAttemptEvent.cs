namespace Content.Shared._Aquila.Chat;

/// <summary>
/// Вызывается перед воспроизведением звука эмоции. Отмена убирает звук, но эмоция остаётся в чате.
/// </summary>
[ByRefEvent]
public record struct EmoteSoundAttemptEvent(EntityUid Source, bool Cancelled = false);
