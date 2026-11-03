using Content.Shared._Aquila.Cyberware.Components;
using Content.Shared._Shitmed.Medical.Surgery;
using Content.Shared._Shitmed.Medical.Surgery.Steps;
using Content.Shared.Implants.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared._Aquila.Surgery;

/// <summary>
/// Хирургическое извлечение подкожных имплантов, которые опознал глубокий скан.
/// </summary>
public sealed class ImplantExtractionSystem : EntitySystem
{
    private static readonly LocId ExtractedMessage = "surgery-popup-implants-extracted";
    private static readonly LocId NothingMessage = "surgery-popup-implants-nothing";

    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryExtractImplantsStepComponent, SurgeryStepEvent>(OnStep);
        SubscribeLocalEvent<SurgeryExtractImplantsStepComponent, SurgeryStepCompleteCheckEvent>(OnCheck);
    }

    private void OnStep(Entity<SurgeryExtractImplantsStepComponent> ent, ref SurgeryStepEvent args)
    {
        if (_net.IsClient)
            return;

        var names = new List<string>();
        foreach (var implant in GetExtractable(args.Body))
        {
            if (!_container.TryGetContainingContainer((implant, null, null), out var container)
                || !_container.Remove(implant, container))
                continue;

            names.Add(Name(implant));
            QueueDel(implant);
        }

        var message = names.Count > 0
            ? Loc.GetString(ExtractedMessage, ("implants", string.Join(", ", names)))
            : Loc.GetString(NothingMessage);

        _popup.PopupEntity(message, args.Body, args.User, PopupType.Medium);
    }

    private void OnCheck(Entity<SurgeryExtractImplantsStepComponent> ent, ref SurgeryStepCompleteCheckEvent args)
    {
        if (GetExtractable(args.Body).Count > 0)
            args.Cancelled = true;
    }

    private List<EntityUid> GetExtractable(EntityUid body)
    {
        var result = new List<EntityUid>();
        if (!TryComp<ImplantsIdentifiedComponent>(body, out var identified)
            || !_container.TryGetContainer(body, ImplanterComponent.ImplantSlotId, out var implants))
            return result;

        foreach (var implant in implants.ContainedEntities)
        {
            if (identified.Identified.Contains(implant)
                && TryComp<SubdermalImplantComponent>(implant, out var subdermal)
                && !subdermal.Permanent)
                result.Add(implant);
        }

        return result;
    }
}
