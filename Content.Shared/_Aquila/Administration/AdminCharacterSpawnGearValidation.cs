using Content.Shared.Clothing.Components;
using Content.Shared.Item;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Administration;

/// <summary>Проверки предметов и состава собранного комплекта.</summary>
public static class AdminCharacterSpawnGearValidation
{
    /// <summary>Предельное число предметов в пользовательском комплекте.</summary>
    public const int MaxItems = 64;

    private const int MaxSlotNameLength = 64;

    /// <summary>Проверяет, что прототип существует и его можно выбрать как предмет комплекта.</summary>
    public static bool CanUseItem(EntProtoId id, IPrototypeManager prototypes, IComponentFactory components)
    {
        return !string.IsNullOrEmpty(id.Id)
            && prototypes.TryIndex(id, out var prototype)
            && CanUseItem(prototype, components);
    }

    /// <summary>Проверяет, что прототип описывает конкретный предмет.</summary>
    public static bool CanUseItem(EntityPrototype prototype, IComponentFactory components)
    {
        return !prototype.Abstract && prototype.TryComp<ItemComponent>(out _, components);
    }

    /// <summary>Checks that the prototype can be worn in an inventory slot.</summary>
    public static bool CanUseEquipmentItem(EntProtoId id, IPrototypeManager prototypes, IComponentFactory components)
    {
        return !string.IsNullOrEmpty(id.Id)
            && prototypes.TryIndex(id, out var prototype)
            && CanUseEquipmentItem(prototype, components);
    }

    /// <summary>Checks that the prototype is a wearable item or a clothing item without the item component.</summary>
    public static bool CanUseEquipmentItem(EntityPrototype prototype, IComponentFactory components)
    {
        return !prototype.Abstract
            && (prototype.TryComp<ItemComponent>(out _, components)
                || prototype.TryComp<ClothingComponent>(out _, components));
    }

    /// <summary>Проверяет состав комплекта до примерки на конкретное тело.</summary>
    public static bool CanUseCustomGear(AdminCharacterSpawnCustomGear gear,
        IPrototypeManager prototypes, IComponentFactory components)
    {
        if (gear.Equipment == null || gear.Inhand == null || gear.Storage == null
            || gear.Equipment.Count > MaxItems || gear.Inhand.Count > MaxItems - gear.Equipment.Count
            || gear.Storage.Count > gear.Equipment.Count)
        {
            return false;
        }

        var remaining = MaxItems - gear.Equipment.Count - gear.Inhand.Count;
        foreach (var (slot, items) in gear.Storage)
        {
            if (items == null || items.Count > remaining || !gear.Equipment.TryGetValue(slot, out var container))
                return false;

            if (items.Count > 0
                && (string.IsNullOrEmpty(container.Id)
                    || !prototypes.TryIndex(container, out var prototype)
                    || !prototype.TryComp<StorageComponent>(out _, components)))
            {
                return false;
            }

            remaining -= items.Count;
        }

        foreach (var (slot, item) in gear.Equipment)
        {
            if (string.IsNullOrWhiteSpace(slot) || slot.Length > MaxSlotNameLength
                || !CanUseEquipmentItem(item, prototypes, components))
            {
                return false;
            }
        }

        foreach (var item in gear.Inhand)
        {
            if (!CanUseItem(item, prototypes, components))
                return false;
        }

        foreach (var items in gear.Storage.Values)
        {
            foreach (var item in items)
            {
                if (!CanUseItem(item, prototypes, components))
                    return false;
            }
        }

        return true;
    }
}
