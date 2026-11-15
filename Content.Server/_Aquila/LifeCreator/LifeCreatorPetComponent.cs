namespace Content.Server.Aquila.LifeCreator;

[RegisterComponent]
public sealed partial class LifeCreatorPetComponent : Component
{
    [ViewVariables]
    public new EntityUid Owner;
}
