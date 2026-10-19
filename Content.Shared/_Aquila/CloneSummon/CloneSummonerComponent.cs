namespace Content.Shared.Aquila.Clone;

[RegisterComponent]
public sealed partial class CloneSummonerComponent : Component
{
    [DataField]
    public List<EntityUid> Clones = new();

    [DataField]
    public EntityUid? TeleportActionEntity;
}
