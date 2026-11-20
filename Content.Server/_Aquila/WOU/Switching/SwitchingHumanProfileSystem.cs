using Content.Server.Humanoid;
using Content.Shared._Aquila.WOU.Switching;
using Content.Shared.DetailExaminable;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;

namespace Content.Server._Aquila.WOU.Switching;

public sealed class SwitchingHumanProfileSystem : EntitySystem
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SwitchingHumanProfileComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, SwitchingHumanProfileComponent comp, MapInitEvent args)
    {
        _humanoid.LoadProfile(uid, comp.Profile);

        if (!string.IsNullOrEmpty(comp.Profile.Name))
            _meta.SetEntityName(uid, comp.Profile.Name);

        if (!string.IsNullOrEmpty(comp.Profile.FlavorText))
        {
            var detail = EnsureComp<DetailExaminableComponent>(uid);
            detail.Content = comp.Profile.FlavorText;
        }

        var coords = _transform.GetMapCoordinates(uid);
        var gear = new Dictionary<string, string>
        {
            { "head", "ClothingHeadHatAquilaBowler" },
            { "jumpsuit", "ClothingUniformJumpsuitAquilaBusiness" },
            { "neck", "ClothingNeckScarfAquilaRed" },
            { "gloves", "ClothingHandsGlovesColorWhite" },
            { "shoes", "ClothingShoesColorWhite" },
            { "id", "PassengerIDCardSatoru" }
        };

        foreach (var (slot, proto) in gear)
        {
            var item = Spawn(proto, coords);
            _inventory.TryEquip(uid, item, slot, silent: true);
        }
    }
}