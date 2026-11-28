using Content.Shared.Clothing.Components;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Inventory;
using Robust.Shared.Containers;

namespace Content.Server.Heretic.Ritual;

/// <summary>
/// Требует заданное число лежащих рядом с платформой предметов одежды, занимающих указанные слоты.
/// </summary>
public sealed partial class RitualClothingSlotRequirementBehavior : RitualCustomBehavior
{
    private static readonly LocId DefaultFailMessage = "heretic-ritual-fail-shoes";

    /// <summary>
    /// Слоты инвентаря, которые должен занимать предмет.
    /// </summary>
    [DataField]
    public SlotFlags Slot = SlotFlags.FEET;

    /// <summary>
    /// Сколько предметов требуется для ритуала.
    /// </summary>
    [DataField]
    public int Count = 1;

    /// <summary>
    /// Ключ локали с сообщением о нехватке предметов.
    /// </summary>
    [DataField]
    public LocId FailMessage = DefaultFailMessage;

    private List<EntityUid> _found = new();

    /// <summary>
    /// Ищет рядом с платформой одежду с нужными слотами и проверяет, что её хватает.
    /// </summary>
    public override bool Execute(RitualData args, out string? outstr)
    {
        _found.Clear();
        outstr = null;

        var lookup = args.EntityManager.System<EntityLookupSystem>();
        var container = args.EntityManager.System<SharedContainerSystem>();

        foreach (var ent in lookup.GetEntitiesInRange(args.Platform, 1.5f, LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (container.IsEntityInContainer(ent))
                continue;

            if (!args.EntityManager.TryGetComponent<ClothingComponent>(ent, out var clothing)
                || (clothing.Slots & Slot) == SlotFlags.NONE)
                continue;

            _found.Add(ent);

            if (_found.Count >= Count)
                break;
        }

        if (_found.Count >= Count)
            return true;

        _found.Clear();
        outstr = Loc.GetString(FailMessage);
        return false;
    }

    /// <summary>
    /// Удаляет найденные предметы после успешного ритуала.
    /// </summary>
    public override void Finalize(RitualData args)
    {
        foreach (var ent in _found)
            args.EntityManager.QueueDeleteEntity(ent);

        _found.Clear();
    }
}
