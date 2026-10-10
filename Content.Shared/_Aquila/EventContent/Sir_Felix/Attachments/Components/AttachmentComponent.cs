namespace Content.Shared._Aquila.EventContent.Sir_Felix.Attachments.Components;

[RegisterComponent]
public sealed partial class AttachmentComponent : Component
{
    [DataField("slotId")]
    public string SlotId { get; set; } = "gun_attachment_right";
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Weapon { get; set; }
}
