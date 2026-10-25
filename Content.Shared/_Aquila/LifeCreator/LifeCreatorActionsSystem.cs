using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Aquila.LifeCreator;

public sealed class LifeCreatorActionsSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    private static readonly EntProtoId BarrierActionId = "ActionSpawnBarrier";
    private static readonly EntProtoId VivifyActionId = "ActionVivifyItem";
    private static readonly EntProtoId VineActionId = "ActionSummonVines";
    private static readonly EntProtoId SacrificeArmActionId = "ActionSacrificeArm";
    private static readonly EntProtoId HealTargetActionId = "ActionHealTarget";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LifeCreatorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LifeCreatorComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, LifeCreatorComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.BarrierActionEntity, BarrierActionId);
        _actions.AddAction(uid, ref component.VivifyActionEntity, VivifyActionId);
        _actions.AddAction(uid, ref component.VineActionEntity, VineActionId);
        _actions.AddAction(uid, ref component.SacrificeArmActionEntity, SacrificeArmActionId);
        _actions.AddAction(uid, ref component.HealTargetActionEntity, HealTargetActionId);
    }

    private void OnShutdown(EntityUid uid, LifeCreatorComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.BarrierActionEntity);
        _actions.RemoveAction(uid, component.VivifyActionEntity);
        _actions.RemoveAction(uid, component.VineActionEntity);
        _actions.RemoveAction(uid, component.SacrificeArmActionEntity);
        _actions.RemoveAction(uid, component.HealTargetActionEntity);
    }
}
