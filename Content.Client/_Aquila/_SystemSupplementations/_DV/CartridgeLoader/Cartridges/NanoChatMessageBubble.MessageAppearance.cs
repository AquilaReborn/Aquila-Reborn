using Content.Client._Aquila.Emoji;
using Content.Client._Aquila.PDA;
using Content.Shared._DV.CartridgeLoader.Cartridges;
using Robust.Client.Graphics;

namespace Content.Client._DV.CartridgeLoader.Cartridges;

public sealed partial class NanoChatMessageBubble
{
    private void ApplyMessageStyle(NanoChatMessage message, bool isOwnMessage)
    {
        var style = PdaStyle.Box(isOwnMessage ? OwnMessageColor : OtherMessageColor);
        style.SetContentMarginOverride(StyleBox.Margin.Horizontal, 10);
        style.SetContentMarginOverride(StyleBox.Margin.Vertical, 6);
        MessagePanel.PanelOverride = style;

        MessageText.SetMessage(EmojiList.Parse(message.Content), [typeof(EmojiTag)], TextColor);

        TimeLabel.HorizontalAlignment = isOwnMessage ? HAlignment.Right : HAlignment.Left;
        TimeLabel.FontColorOverride = PdaStyle.TextMuted;
    }

    /// <summary>
    /// Показывает время отправки под сообщением или скрывает его, если время не передано.
    /// </summary>
    public void SetTime(string? time)
    {
        TimeLabel.Text = time ?? string.Empty;
        TimeLabel.Visible = time != null;
    }
}
