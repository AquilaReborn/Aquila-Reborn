using Content.Server._Aquila.Hydrakin.Components;
using Content.Server._Obelisk.Species.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Hydrakin.Systems;

/// <summary>
/// Переключает мощность пассивного нагрева гидракина в зависимости от верхней одежды и температуры среды.
/// </summary>
public sealed class HydrakinHeatRegulationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    private const string OuterClothingSlot = "outerClothing";

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HydrakinComponent, MapInitEvent>(OnHydrakinMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<HydrakinComponent, HydrakinHeatRegulationComponent, PassiveHeatGenerationComponent>();

        while (query.MoveNext(out var uid, out _, out var regulation, out var passive))
        {
            passive.Watts = IsHeated(uid, regulation) ? regulation.HeatedWatts : regulation.NormalWatts;
        }
    }

    private void OnHydrakinMapInit(Entity<HydrakinComponent> ent, ref MapInitEvent args)
    {
        EnsureComp<HydrakinHeatRegulationComponent>(ent);
    }

    private bool IsHeated(EntityUid uid, HydrakinHeatRegulationComponent regulation)
    {
        if (_inventory.TryGetSlotEntity(uid, OuterClothingSlot, out _))
            return true;

        var mixture = _atmosphere.GetTileMixture(uid);
        return mixture != null && mixture.Temperature > regulation.HotEnvironmentTemperature;
    }
}
