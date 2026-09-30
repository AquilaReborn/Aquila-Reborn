using Robust.Shared.GameStates;

namespace Content.Shared._Aquila.OocNotes;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OocNotesComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Content = string.Empty;
}
