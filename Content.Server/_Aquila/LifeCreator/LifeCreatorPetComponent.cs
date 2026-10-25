namespace Content.Server.Aquila.LifeCreator;

[RegisterComponent]
public sealed partial class LifeCreatorPetComponent : Component
{
    [ViewVariables]
    public EntityUid Owner;
}
