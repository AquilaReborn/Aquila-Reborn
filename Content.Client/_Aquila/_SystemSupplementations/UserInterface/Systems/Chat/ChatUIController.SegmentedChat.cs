using System.Collections.Generic;
using Content.Client._Aquila.Chat.Controls;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Shared.Chat;
using Content.Shared.Radio;

namespace Content.Client.UserInterface.Systems.Chat;

public sealed partial class ChatUIController
{
    private void InitializeAquila()
    {
        _net.RegisterNetMessage<MsgSegmentedChatMessage>();
    }

    private AquilaSegmentedMessage? CaptureAquilaMessage(ChatBox box)
    {
        if (box.ChatInput.Input is not LanguageLineEdit input)
            return null;

        var segments = input.GetSegments();
        if (segments.Count == 0)
            return null;

        return new AquilaSegmentedMessage(input.Text, segments);
    }

    private bool TrySendAquilaSegmentedMessage(
        AquilaSegmentedMessage? message,
        ChatSelectChannel prefixChannel,
        ChatSelectChannel channel)
    {
        if (message == null)
            return false;

        string? radioChannel;

        if (channel == ChatSelectChannel.Radio)
        {
            if (prefixChannel == ChatSelectChannel.None)
            {
                radioChannel = _prototypeManager.Index<RadioChannelPrototype>(SharedChatSystem.CommonChannel).ID;
            }
            else
            {
                if (_chatSys == null)
                    return false;

                if (_player.LocalEntity is not EntityUid { Valid: true } player)
                    return false;

                if (!_chatSys.TryProcessRadioMessage(player, message.Text, out var output, out var radio, quiet: true) ||
                    radio == null)
                    return false;

                if (output.Length == 0)
                    return false;

                radioChannel = radio.ID;
                message = ShiftAquilaSegments(message, output);

                if (message.Segments.Count == 0)
                    return false;
            }
        }
        else
        {
            if (prefixChannel != ChatSelectChannel.None)
                return false;

            if (channel != ChatSelectChannel.Local && channel != ChatSelectChannel.Whisper)
                return false;

            radioChannel = null;
        }

        _net.ClientSendMessage(new MsgSegmentedChatMessage
        {
            Text = message.Text,
            Whisper = channel == ChatSelectChannel.Whisper,
            RadioChannel = radioChannel,
            Segments = new List<ChatMessageSegment>(message.Segments)
        });

        return true;
    }

    private static AquilaSegmentedMessage ShiftAquilaSegments(AquilaSegmentedMessage message, string text)
    {
        var removed = message.Text.Length - text.Length;
        var segments = new List<ChatMessageSegment>(message.Segments.Count);

        foreach (var segment in message.Segments)
        {
            var start = segment.Start - removed;
            var end = segment.Start + segment.Length - removed;

            if (end <= 0 || start >= text.Length)
                continue;

            start = Math.Max(start, 0);
            end = Math.Min(end, text.Length);

            if (end <= start)
                continue;

            segments.Add(new ChatMessageSegment
            {
                Start = start,
                Length = end - start,
                Language = segment.Language
            });
        }

        return new AquilaSegmentedMessage(text, segments);
    }

    private sealed record AquilaSegmentedMessage(string Text, List<ChatMessageSegment> Segments);
}
