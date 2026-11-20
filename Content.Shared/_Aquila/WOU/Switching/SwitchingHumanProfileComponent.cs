using Content.Shared.Preferences;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.WOU.Switching;

[RegisterComponent]
public sealed partial class SwitchingHumanProfileComponent : Component
{
    [DataField] public HumanoidCharacterProfile Profile = new();

    [DataField]
    public Dictionary<string, EntProtoId> Gear = new()
    {
        { "head", "ClothingHeadHatAquilaBowler" },
        { "jumpsuit", "ClothingUniformJumpsuitAquilaBusiness" },
        { "neck", "ClothingNeckScarfAquilaRed" },
        { "gloves", "ClothingHandsGlovesColorWhite" },
        { "shoes", "ClothingShoesColorWhite" },
        { "id", "PassengerIDCardSatoru" }
    };
}