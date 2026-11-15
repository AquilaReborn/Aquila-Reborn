using System.Numerics;
using Content.Shared._DV.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._DV.CartridgeLoader.Cartridges;

public sealed partial class NanoChatLookupView
{
    private static readonly Vector2 StartChatButtonSize = new(32, 32);

    private static (Label Name, Label Number, Button StartChat) CreateContactControls(NanoChatRecipient contact)
    {
        var nameLabel = new Label()
        {
            Text = contact.Name,
            HorizontalExpand = true,
            ClipText = true,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(4, 0, 4, 0),
        };
        var numberLabel = new Label()
        {
            Text = $"#{contact.Number:D4}",
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0),
        };
        var startChatButton = new Button()
        {
            Text = "+",
            MinSize = StartChatButtonSize,
            MaxSize = StartChatButtonSize,
            ToolTip = Loc.GetString("nano-chat-new-chat"),
        };

        return (nameLabel, numberLabel, startChatButton);
    }

    private static BoxContainer CreateContactRow(Label nameLabel, Label numberLabel, Button startChatButton)
    {
        var row = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
        };
        row.AddChild(nameLabel);
        row.AddChild(numberLabel);
        row.AddChild(startChatButton);
        return row;
    }
}
