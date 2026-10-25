namespace Content.Server.Aquila.LifeCreator;

[RegisterComponent]
public sealed partial class VivifiedCreatureComponent : Component
{
    public const string ParkedItemContainerId = "vivify_parked_item";

    [ViewVariables]
    public EntityUid Caster;

    [ViewVariables]
    public EntityUid ParkedItem;
}
