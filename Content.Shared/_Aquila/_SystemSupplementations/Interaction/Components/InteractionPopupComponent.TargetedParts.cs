using Content.Shared._Shitmed.Targeting;

namespace Content.Shared.Interaction.Components;

public sealed partial class InteractionPopupComponent
{
    /// <summary>
    ///     Localized success messages for specific targeted body parts, overriding <see cref="InteractSuccessString"/>.
    /// </summary>
    [DataField("bodyPartSuccessStrings")]
    public Dictionary<TargetBodyPart, string> BodyPartSuccessStrings = new();

    /// <summary>
    ///     Localized messages perceived by others for specific targeted body parts, overriding <see cref="MessagePerceivedByOthers"/>.
    /// </summary>
    [DataField("bodyPartOthersStrings")]
    public Dictionary<TargetBodyPart, string> BodyPartOthersStrings = new();
}
