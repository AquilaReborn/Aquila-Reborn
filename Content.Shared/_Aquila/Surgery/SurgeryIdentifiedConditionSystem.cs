using Content.Shared._Aquila.Cyberware.Components;
using Content.Shared._Shitmed.Medical.Surgery.Conditions;
using Content.Shared.Body.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._Aquila.Surgery;

/// <summary>
/// Прячет операции извлечения скрытых имплантов, пока глубокий скан не опознал эти импланты.
/// Владелец, который оперирует сам себя, знает о своих имплантах и обходится без скана.
/// </summary>
public sealed class SurgeryIdentifiedConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryIdentifiedConditionComponent, SurgeryValidEvent>(OnSurgeryValid);
    }

    private void OnSurgeryValid(Entity<SurgeryIdentifiedConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (args.Cancelled || args.User == args.Body)
            return;

        if (ent.Comp.Part && !IsIdentified(args.Body, args.Part))
        {
            args.Cancelled = true;
            return;
        }

        if (ent.Comp.OrganSlot is not { } slot
            || !_container.TryGetContainer(args.Part, SharedBodySystem.GetOrganContainerId(slot), out var container))
            return;

        foreach (var organ in container.ContainedEntities)
        {
            if (!HasComp<IdentificationRequiredComponent>(organ) || IsIdentified(args.Body, organ))
                continue;

            args.Cancelled = true;
            return;
        }
    }

    /// <summary>
    /// Опознал ли глубокий скан этот имплант, орган или часть тела в теле существа.
    /// </summary>
    public bool IsIdentified(Entity<ImplantsIdentifiedComponent?> body, EntityUid implant)
    {
        if (!Resolve(body, ref body.Comp, false))
            return false;

        return body.Comp.Identified.Contains(implant);
    }
}
