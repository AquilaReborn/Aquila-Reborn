using Robust.Shared.Serialization;

namespace Content.Shared.Chat;

/// <summary>
/// Участок текста сообщения, набранный на отдельном языке.
/// Смещения считаются в символах исходного текста в том виде, в каком его набрал игрок.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChatMessageSegment
{
    /// <summary>
    /// Позиция начала участка в исходном тексте, в символах.
    /// </summary>
    public int Start;

    /// <summary>
    /// Длина участка в символах.
    /// </summary>
    public int Length;

    /// <summary>
    /// Идентификатор прототипа языка, на котором набран участок.
    /// </summary>
    public string Language = string.Empty;
}
