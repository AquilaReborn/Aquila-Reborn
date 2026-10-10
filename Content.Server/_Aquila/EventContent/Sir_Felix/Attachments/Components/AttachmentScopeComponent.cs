namespace Content.Server._Aquila.EventContent.Sir_Felix.Attachments.Components;

[RegisterComponent]
public sealed partial class AttachmentScopeComponent : Component
{
    [DataField("slotId")]
    public string SlotId { get; set; } = "gun_attachment_top";

    [DataField]
    public float MaxOffset = 3f;
    [DataField]
    public float OffsetSpeed = 0.5f;
    [DataField]
    public float PvsIncrease = 0.3f;
}
