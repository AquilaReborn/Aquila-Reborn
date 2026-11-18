using Content.Goobstation.Shared.Clothing.Components;
using Content.Shared._Aquila.Inventory.Components;
using Content.Shared.Clothing;
using Content.Shared.Item.ItemToggle;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager;

namespace Content.Goobstation.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private ItemToggleSystem _itemToggle = default!;
    [Dependency] private ISerializationManager _serializationManager = default!;

    private const string GlovesSlot = "gloves";
    private const string ShoesSlot = "shoes";

    /// <summary>
    /// Components copied from the stored item to the modsuit part so its functions are not lost.
    /// </summary>
    private static readonly Type[] StoredEffectComponents =
    {
        typeof(ClothingGrantComponentComponent),
        typeof(MagbootsComponent),
    };

    /// <summary>
    /// Copies the stored item's effects to the modsuit part before it is equipped.
    /// </summary>
    private void TransferStoredEffects(EntityUid stored, EntityUid part, string slot)
    {
        if (slot != GlovesSlot && slot != ShoesSlot)
            return;

        if (!_net.IsServer || TerminatingOrDeleted(stored) || TerminatingOrDeleted(part))
            return;

        var tracked = EnsureComp<StoredClothingComponent>(part);
        tracked.Item = stored;
        Dirty(part, tracked);

        foreach (var type in StoredEffectComponents)
        {
            if (HasComp(part, type)
                || !TryComp(stored, type, out var component))
                continue;

            if (type == typeof(MagbootsComponent) && !_itemToggle.IsActivated(stored))
                continue;

            var copy = (Component) _serializationManager.CreateCopy(component!, notNullableOverride: true);
            AddComp(part, copy);
            tracked.Components.Add(_componentFactory.GetRegistration(type).Name);
        }
    }

    /// <summary>
    /// Removes the components copied from the stored item from the modsuit part.
    /// </summary>
    private void RemoveStoredEffects(EntityUid part)
    {
        if (!_net.IsServer || TerminatingOrDeleted(part) || !TryComp<StoredClothingComponent>(part, out var tracked))
            return;

        foreach (var name in tracked.Components)
        {
            if (_componentFactory.TryGetRegistration(name, out var registration, ignoreCase: true))
                RemComp(part, registration.Type);
        }

        RemCompDeferred<StoredClothingComponent>(part);
    }
}
