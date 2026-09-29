using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared._Aquila.ArmorPlate;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StorageArmorPlateComponent : Component
{
    [DataField, AutoNetworkedField]
    public int MaxPlates = 2;

    [DataField, AutoNetworkedField]
    public bool CanRemovePlates = true;

    [DataField]
    public string PlateTag = "ArmorPlate";

    [DataField]
    public SoundSpecifier PlateSound = new SoundPathSpecifier("/Audio/Items/jumpsuit_equip.ogg");

    [DataField]
    public VerbCategory? RemovePlateCategory;

    [DataField]
    public string ContainerId = "storage-armor-plate";

    [ViewVariables]
    public Container Storage = default!;

    [DataField, AutoNetworkedField]
    public TimeSpan InsertDelay = TimeSpan.FromSeconds(3);
}
