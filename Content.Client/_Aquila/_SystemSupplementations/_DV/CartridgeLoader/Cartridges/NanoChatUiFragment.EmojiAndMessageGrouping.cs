using System.Numerics;
using Content.Client._Aquila.Emoji;
using Content.Client.GameTicking.Managers;
using Content.Shared._DV.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._DV.CartridgeLoader.Cartridges;

public sealed partial class NanoChatUiFragment
{
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;

    private void InitializeEmojiPanel()
    {
        EmojiButton.OnToggled += args => EmojiPanel.Visible = args.Pressed;
        foreach (var name in EmojiList.Names)
        {
            if (!EmojiList.TryGetPath(name, out var path))
                continue;

            var button = new Button
            {
                MinSize = new Vector2(30, 30),
                Margin = new Thickness(1),
                ToolTip = $":{name}:",
                StyleClasses = { "ButtonSquare", "PdaListEntry" },
            };
            button.AddChild(new TextureRect
            {
                TexturePath = path.ToString(),
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
                SetSize = new Vector2(22, 22),
            });
            button.OnPressed += _ =>
            {
                MessageInput.InsertAtCursor($":{name}:");
                MessageInput.GrabKeyboardFocus();
            };
            EmojiGrid.AddChild(button);
        }
    }

    private void UpdateEmojiPanel(bool hasActiveChat)
    {
        if (!hasActiveChat)
        {
            EmojiButton.Pressed = false;
            EmojiPanel.Visible = false;
        }
    }

    private void AddMessageBubbles(List<NanoChatMessage> chatMessages)
    {
        var roundStart = _entitySystem.GetEntitySystem<ClientGameTicker>().RoundStartTimeSpan;
        for (var i = 0; i < chatMessages.Count; i++)
        {
            var message = chatMessages[i];
            var messageBubble = new NanoChatMessageBubble();
            messageBubble.SetMessage(message, message.SenderId == _ownNumber);

            var isLastInGroup = i == chatMessages.Count - 1
                || chatMessages[i + 1].SenderId != message.SenderId
                || chatMessages[i + 1].Timestamp - message.Timestamp > TimeSpan.FromMinutes(5);

            if (isLastInGroup)
            {
                var time = message.Timestamp - roundStart;
                messageBubble.SetTime(time < TimeSpan.Zero ? null : time.ToString(@"hh\:mm"));
            }

            MessageList.AddChild(messageBubble);
            MessageList.AddChild(new Control { MinSize = new Vector2(0, isLastInGroup ? 8 : 2) });
        }
    }
}
