using Content.Server._EinsteinEngines.Language;
using Content.Shared._Aquila.Language;
using Content.Shared._Aquila.Language.Components;
using Content.Shared.Implants.Components;
using Robust.Shared.Containers;

namespace Content.Server._Aquila.Language;

/// <summary>
/// Обновляет знание языков владельца, когда в его языковой имплант вставляют или вынимают чипы.
/// </summary>
public sealed class TranslatorChipSystem : SharedTranslatorChipSystem
{
    [Dependency] private readonly LanguageSystem _language = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TranslatorChipHolderComponent, EntInsertedIntoContainerMessage>(OnChipInserted);
        SubscribeLocalEvent<TranslatorChipHolderComponent, EntRemovedFromContainerMessage>(OnChipRemoved);
        SubscribeLocalEvent<TranslatorChipHolderComponent, EntGotInsertedIntoContainerMessage>(OnHolderImplanted);
        SubscribeLocalEvent<TranslatorChipHolderComponent, EntGotRemovedFromContainerMessage>(OnHolderRemoved);
    }

    private void OnChipInserted(Entity<TranslatorChipHolderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == TranslatorChipHolderComponent.ContainerId)
            UpdateImplantee(ent);
    }

    private void OnChipRemoved(Entity<TranslatorChipHolderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == TranslatorChipHolderComponent.ContainerId)
            UpdateImplantee(ent);
    }

    private void OnHolderImplanted(Entity<TranslatorChipHolderComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ImplanterComponent.ImplantSlotId)
            _language.UpdateEntityLanguages(args.Container.Owner);
    }

    private void OnHolderRemoved(Entity<TranslatorChipHolderComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ImplanterComponent.ImplantSlotId)
            _language.UpdateEntityLanguages(args.Container.Owner);
    }

    private void UpdateImplantee(Entity<TranslatorChipHolderComponent> ent)
    {
        if (TryComp<SubdermalImplantComponent>(ent, out var implant) && implant.ImplantedEntity is { } implantee)
            _language.UpdateEntityLanguages(implantee);
    }
}
