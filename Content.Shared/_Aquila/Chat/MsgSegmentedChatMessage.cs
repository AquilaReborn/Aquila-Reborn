using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat;

/// <summary>
/// Сообщение клиента с текстом локального сообщения и участками, размеченными языками.
/// </summary>
public sealed class MsgSegmentedChatMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    private const int MaxSegments = 256;

    /// <summary>
    /// Исходный текст сообщения в том виде, в каком его набрал игрок.
    /// </summary>
    public string Text = string.Empty;

    /// <summary>
    /// Признак того, что сообщение отправлено шёпотом, а не обычной речью.
    /// </summary>
    public bool Whisper;

    /// <summary>
    /// Идентификатор прототипа радиочастоты, если сообщение уходит в радио. Для обычной речи null.
    /// </summary>
    public string? RadioChannel;

    /// <summary>
    /// Участки исходного текста, помеченные языками.
    /// </summary>
    public List<ChatMessageSegment> Segments = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Text = buffer.ReadString();
        Whisper = buffer.ReadBoolean();

        var radioChannel = buffer.ReadString();
        RadioChannel = radioChannel.Length == 0 ? null : radioChannel;

        var count = buffer.ReadVariableInt32();
        buffer.AssertAtLeastSize(count * (NetMessageExt.MinSizeOfVariableInt * 2 + NetMessageExt.MinSizeOfString));

        if (count > MaxSegments)
            throw new InvalidOperationException($"Too many chat message segments: {count}.");

        Segments.EnsureCapacity(count);

        for (var i = 0; i < count; i++)
        {
            Segments.Add(new ChatMessageSegment
            {
                Start = buffer.ReadVariableInt32(),
                Length = buffer.ReadVariableInt32(),
                Language = buffer.ReadString(),
            });
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Text);
        buffer.Write(Whisper);
        buffer.Write(RadioChannel ?? string.Empty);

        buffer.WriteVariableInt32(Segments.Count);

        foreach (var segment in Segments)
        {
            buffer.WriteVariableInt32(segment.Start);
            buffer.WriteVariableInt32(segment.Length);
            buffer.Write(segment.Language);
        }
    }
}
