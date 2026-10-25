namespace Content.Shared.Aquila.LifeCreator;

[RegisterComponent]
public sealed partial class LifeCreatorComponent : Component
{
    [ViewVariables]
    public EntityUid? BarrierActionEntity;

    [ViewVariables]
    public EntityUid? VivifyActionEntity;

    [ViewVariables]
    public EntityUid? VineActionEntity;

    [ViewVariables]
    public EntityUid? SacrificeArmActionEntity;

    [ViewVariables]
    public EntityUid? HealTargetActionEntity;
}
