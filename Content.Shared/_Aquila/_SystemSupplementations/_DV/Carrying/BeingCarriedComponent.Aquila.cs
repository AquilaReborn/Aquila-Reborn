using Robust.Shared.GameStates;

namespace Content.Shared._DV.Carrying;

public sealed partial class BeingCarriedComponent
{
    [AutoNetworkedField]
    public bool OwnsKnockdown;
}
