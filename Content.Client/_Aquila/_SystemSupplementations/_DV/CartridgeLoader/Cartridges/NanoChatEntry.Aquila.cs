using Content.Client._Aquila.PDA;
using Content.Shared._DV.CartridgeLoader.Cartridges;

namespace Content.Client._DV.CartridgeLoader.Cartridges;

public sealed partial class NanoChatEntry
{
    private const string UnknownAvatarLetter = "?";

    private void InitializeAquilaStyle()
    {
        UnreadIndicator.PanelOverride = PdaStyle.Box(PdaStyle.Red);
        NameLabel.FontColorOverride = PdaStyle.TextHeader;
        AvatarLabel.FontColorOverride = Color.White;
    }

    private void UpdateAvatar(NanoChatRecipient recipient, uint number, bool isSelected)
    {
        Avatar.PanelOverride = PdaStyle.Box(PdaStyle.AvatarColors[number % PdaStyle.AvatarColors.Length]);
        AvatarLabel.Text = string.IsNullOrWhiteSpace(recipient.Name)
            ? UnknownAvatarLetter
            : char.ToUpperInvariant(recipient.Name.TrimStart()[0]).ToString();

        ChatButton.ModulateSelfOverride = isSelected ? PdaStyle.Selected : null;
    }
}
