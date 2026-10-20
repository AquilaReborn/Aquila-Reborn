using Content.Shared.Chat;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared._Aquila.TypingIndicator.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.TypingIndicator.Components;

/// <summary>
/// Индикаторы печатания над персонажем для отдельных каналов чата и для письма на бумаге.
/// Добавляется автоматически, когда игрок вселяется в сущность.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTypingIndicatorChannelSystem))]
public sealed partial class TypingIndicatorChannelComponent : Component
{
    /// <summary>
    /// Индикатор, который показывается вместо обычного, пока игрок печатает в указанный канал.
    /// Для каналов без записи остаётся обычный индикатор.
    /// </summary>
    [DataField]
    public Dictionary<ChatSelectChannel, ProtoId<TypingIndicatorPrototype>> Indicators = new()
    {
        [ChatSelectChannel.Whisper] = "ChannelWhisper",
        [ChatSelectChannel.Radio] = "ChannelRadio",
        [ChatSelectChannel.Emotes] = "ChannelEmote",
        [ChatSelectChannel.LOOC] = "ChannelOoc",
        [ChatSelectChannel.OOC] = "ChannelOoc",
        [ChatSelectChannel.Admin] = "ChannelOoc",
    };

    /// <summary>
    /// Индикатор, который показывается, пока персонаж пишет на бумаге и не печатает в чат.
    /// </summary>
    [DataField]
    public ProtoId<TypingIndicatorPrototype> WritingIndicator = "WritingPaper";
}
