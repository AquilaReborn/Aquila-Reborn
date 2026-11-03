using Robust.Shared.Prototypes;
namespace Content.Server._Aquila.EventContent.Sir_Felix.Attachments.Components;

[RegisterComponent]
public sealed partial class AttachmentFlashlightComponent : Component
{
    [DataField("slotId")]
    public string SlotId { get; set; } = "gun_attachment_right";
    [ViewVariables(VVAccess.ReadWrite)]
    public bool LightOn { get; set; } = false;

    [DataField("radius")]
    public float Radius { get; set; } = 6f;

    [DataField("toggleAction")]
    public EntProtoId ToggleAction = "ActionToggleAttachmentFlashlight";

    [DataField("toggleActionEntity")]
    public EntityUid? ToggleActionEntity;
}
