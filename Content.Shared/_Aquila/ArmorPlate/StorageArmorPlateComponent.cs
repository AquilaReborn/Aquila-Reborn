using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.ArmorPlate;

/// <summary>
/// Позволяет вкладывать в комбезы бронепластины, которые поглощают часть входящего урона вместо носителя.
/// </summary>
[RegisterComponent]
public sealed partial class StorageArmorPlateComponent : Component
{
    public const string ContainerId = "storage-armor-plate";

    [DataField]
    public int MaxPlates = 2;

    [DataField]
    public bool CanRemovePlates = true;

    [DataField]
    public ProtoId<TagPrototype> PlateTag = "ArmorPlate";

    [DataField]
    public SoundSpecifier PlateSound = new SoundPathSpecifier("/Audio/Items/jumpsuit_equip.ogg");

    [DataField]
    public TimeSpan InsertDelay = TimeSpan.FromSeconds(3);

    [ViewVariables]
    public Container Storage = default!;
}
