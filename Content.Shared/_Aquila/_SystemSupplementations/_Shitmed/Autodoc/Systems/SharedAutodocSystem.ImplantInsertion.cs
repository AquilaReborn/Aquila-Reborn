using System.Linq;
using Content.Shared._Shitmed.Autodoc.Components;
using Content.Shared._Shitmed.Medical.Surgery.Conditions;
using Content.Shared._Shitmed.Medical.Surgery.Steps;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Hands.Components;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Autodoc.Systems;

public abstract partial class SharedAutodocSystem
{
    private const string ItemUnavailableError = "item-unavailable";

    private void FreeItemHand(Entity<AutodocComponent, HandsComponent> ent, EntityUid item)
    {
        if (_hands.GetHeldItem((ent.Owner, ent.Comp2), ent.Comp1.ItemSlot) is not { } held || held == item)
            return;

        _storage.Insert(ent, held, out _);
    }

    private void PrepareInsertItem(Entity<AutodocComponent> ent, EntityUid surgery, EntProtoId stepId)
    {
        if (_surgery.GetSingleton(stepId) is not { } step
            || !TryComp<HandsComponent>(ent, out var hands)
            || !TryComp<StorageComponent>(ent, out var storage))
            return;

        bool isPart;
        if (HasComp<SurgeryAddPartStepComponent>(step) && HasComp<SurgeryPartRemovedConditionComponent>(surgery))
            isPart = true;
        else if (HasComp<SurgeryAddOrganStepComponent>(step) && HasComp<SurgeryOrganConditionComponent>(surgery))
            isPart = false;
        else
            return;

        if (_hands.GetHeldItem((ent.Owner, hands), ent.Comp.ItemSlot) is { } held && IsInsertItem(surgery, held, isPart))
            return;

        EntityUid? found = null;
        foreach (var item in storage.Container.ContainedEntities)
        {
            if (!IsInsertItem(surgery, item, isPart))
                continue;

            found = item;
            break;
        }

        if (found is not { } insert)
            throw new AutodocError(ItemUnavailableError);

        GrabItemOrThrow((ent.Owner, ent.Comp, hands), insert);
    }

    private bool IsInsertItem(EntityUid surgery, EntityUid item, bool isPart)
    {
        if (isPart)
        {
            return TryComp<SurgeryPartRemovedConditionComponent>(surgery, out var removed)
                && TryComp<BodyPartComponent>(item, out var part)
                && part.PartType == removed.Part
                && (removed.Symmetry == null || part.Symmetry == removed.Symmetry);
        }

        return TryComp<SurgeryOrganConditionComponent>(surgery, out var condition)
            && condition.Organ?.Values.FirstOrDefault() is { } organ
            && HasComp<OrganComponent>(item)
            && HasComp(item, organ.Component.GetType());
    }
}
