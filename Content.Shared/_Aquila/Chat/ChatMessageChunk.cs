namespace Content.Shared.Chat;

/// <summary>
/// Санитизированный кусок сообщения: готовый текст и идентификатор языка, на котором он набран.
/// В отличие от <see cref="ChatMessageSegment"/> не содержит смещений в исходном тексте.
/// </summary>
public sealed class ChatMessageChunk
{
    /// <summary>
    /// Готовый текст куска после санитайза и акцентов речи.
    /// </summary>
    public string Text = string.Empty;

    /// <summary>
    /// Идентификатор прототипа языка, на котором набран кусок.
    /// </summary>
    public string Language = string.Empty;
}
