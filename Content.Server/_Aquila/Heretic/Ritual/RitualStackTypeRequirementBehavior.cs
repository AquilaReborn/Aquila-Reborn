using Content.Shared.Heretic.Prototypes;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Ritual;

/// <summary>
/// Требует заданное число предметов указанного типа стака, суммируя несколько стаков.
/// </summary>
public sealed partial class RitualStackTypeRequirementBehavior : RitualCustomBehavior
{
    private static readonly ProtoId<StackPrototype> DefaultStackType = "Silver";

    private static readonly LocId DefaultFailMessage = "heretic-ritual-fail-silver";

    /// <summary>
    /// Тип стака, предметы которого требуются для ритуала.
    /// </summary>
    [DataField]
    public ProtoId<StackPrototype> StackType = DefaultStackType;

    /// <summary>
    /// Сколько единиц стака требуется для ритуала.
    /// </summary>
    [DataField]
    public int Count = 2;

    /// <summary>
    /// Ключ локали с сообщением о нехватке предметов.
    /// </summary>
    [DataField]
    public LocId FailMessage = DefaultFailMessage;

    private List<Entity<StackComponent>> _found = new();

    /// <summary>
    /// Ищет рядом с платформой стаки нужного типа и проверяет, что их сумма достаточна.
    /// </summary>
    public override bool Execute(RitualData args, out string? outstr)
    {
        _found.Clear();
        outstr = null;

        var lookup = args.EntityManager.System<EntityLookupSystem>();
        var container = args.EntityManager.System<SharedContainerSystem>();

        var available = 0;
        foreach (var ent in lookup.GetEntitiesInRange(args.Platform, 1.5f, LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (container.IsEntityInContainer(ent))
                continue;

            if (!args.EntityManager.TryGetComponent<StackComponent>(ent, out var stack)
                || stack.StackTypeId != StackType)
                continue;

            _found.Add((ent, stack));
            available += stack.Count;
        }

        if (available >= Count)
            return true;

        _found.Clear();
        outstr = Loc.GetString(FailMessage);
        return false;
    }

    /// <summary>
    /// Списывает нужное число единиц из найденных стаков.
    /// </summary>
    public override void Finalize(RitualData args)
    {
        var stackSystem = args.EntityManager.System<SharedStackSystem>();
        var remaining = Count;

        foreach (var ent in _found)
        {
            if (remaining <= 0)
                break;

            if (!args.EntityManager.EntityExists(ent.Owner))
                continue;

            var used = Math.Min(ent.Comp.Count, remaining);
            stackSystem.SetCount(ent.AsNullable(), ent.Comp.Count - used);
            remaining -= used;
        }

        _found.Clear();
    }
}
