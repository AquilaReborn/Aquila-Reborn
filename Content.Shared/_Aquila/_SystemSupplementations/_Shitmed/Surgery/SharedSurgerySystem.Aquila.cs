using System.Linq;
using Content.Shared._Aquila.Surgery;
using Content.Shared._Shitmed.Medical.Surgery.Conditions;
using Content.Shared._Shitmed.Medical.Surgery.Steps;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Medical.Surgery;

public abstract partial class SharedSurgerySystem
{
    private static readonly LocId DuplicateAugmentMessage = "surgery-popup-augment-duplicate";
    private static readonly LocId RemoveOrganFailedMessage = "surgery-popup-step-SurgeryStepRemoveOrgan-failed";

    [Dependency] private readonly SharedContainerSystem _containers = default!;

    private bool TryValidateAugmentSlot(Entity<SurgeryOrganConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (!TryComp<SurgeryAugmentSlotComponent>(ent, out var slot))
            return false;

        if (!ent.Comp.Inverse && GetOrganInSlot(args.Part, slot.Slot) == null)
            args.Cancelled = true;

        return true;
    }

    private string GetInsertSlot(EntityUid surgery, OrganComponent organ)
    {
        return TryComp<SurgeryAugmentSlotComponent>(surgery, out var slot) ? slot.Slot : organ.SlotId;
    }

    private bool IsDuplicateAugment(EntityUid surgery, EntityUid part, EntityUid tool, EntityUid user)
    {
        if (!HasComp<SurgeryAugmentSlotComponent>(surgery)
            || MetaData(tool).EntityPrototype is not { } toolProto)
            return false;

        foreach (var (organ, _) in _body.GetPartOrgans(part))
        {
            if (organ == tool
                || MetaData(organ).EntityPrototype is not { } organProto
                || !IsSameAugment(toolProto, organProto))
                continue;

            _popup.PopupClient(Loc.GetString(DuplicateAugmentMessage), user, user);
            return true;
        }

        return false;
    }

    private bool TryCheckAugmentSlotFilled(EntityUid surgery, EntityUid part, ref SurgeryStepCompleteCheckEvent args)
    {
        if (!TryComp<SurgeryAugmentSlotComponent>(surgery, out var slot))
            return false;

        if (GetOrganInSlot(part, slot.Slot) == null)
            args.Cancelled = true;

        return true;
    }

    private bool TryRemoveAugmentFromSlot(EntityUid surgery, EntityUid part, EntityUid user)
    {
        if (!TryComp<SurgeryAugmentSlotComponent>(surgery, out var slot))
            return false;

        if (GetOrganInSlot(part, slot.Slot) is not { } organ)
            return true;

        if (_body.RemoveOrgan(organ))
            _hands.TryPickupAnyHand(user, organ);
        else
            _popup.PopupClient(Loc.GetString(RemoveOrganFailedMessage), user, user);

        return true;
    }

    private bool TryCheckAugmentSlotEmptied(EntityUid surgery, EntityUid part, ref SurgeryStepCompleteCheckEvent args)
    {
        if (!TryComp<SurgeryAugmentSlotComponent>(surgery, out var slot))
            return false;

        if (GetOrganInSlot(part, slot.Slot) != null)
            args.Cancelled = true;

        return true;
    }

    private EntityUid? GetOrganInSlot(EntityUid part, string slot)
    {
        if (!_containers.TryGetContainer(part, SharedBodySystem.GetOrganContainerId(slot), out var container))
            return null;

        return container.ContainedEntities.Count > 0 ? container.ContainedEntities[0] : null;
    }

    private static bool IsSameAugment(EntityPrototype first, EntityPrototype second)
    {
        return first.ID == second.ID
            || first.Parents?.Contains(second.ID) == true
            || second.Parents?.Contains(first.ID) == true;
    }
}
