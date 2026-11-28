using Content.Goobstation.Shared.PhaseShift;
using Content.Server._Aquila.Heretic.Conjurations.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Heretic.Conjurations.Systems;

/// <summary>
/// Обрабатывает душу тигля: уводит носителя в фазовый сдвиг, возвращает его на место и выдаёт кулдаун.
/// </summary>
public sealed class CrucibleSoulSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    private static readonly EntProtoId CooldownStatusEffect = "StatusEffectCrucibleSoulCooldown";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrucibleSoulComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CrucibleSoulComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<CrucibleSoulComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.ReturnCoordinates = _transform.GetMapCoordinates(ent);
        EnsureComp<PhaseShiftedComponent>(ent);
    }

    private void OnShutdown(Entity<CrucibleSoulComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        RemComp<PhaseShiftedComponent>(ent);
        _transform.SetMapCoordinates(ent, ent.Comp.ReturnCoordinates);
        _status.TryAddStatusEffectDuration(ent, CooldownStatusEffect, ent.Comp.CooldownDuration);
    }
}
