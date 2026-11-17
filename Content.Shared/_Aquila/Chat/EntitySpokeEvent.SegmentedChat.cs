namespace Content.Shared.Chat;

public sealed partial class EntitySpokeEvent
{
    /// <summary>
    /// Куски сообщения с языками, если игрок разметил участки. Для обычной речи null.
    /// </summary>
    public IReadOnlyList<ChatMessageChunk>? Chunks;
}
