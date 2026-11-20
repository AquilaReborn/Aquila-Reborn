using Content.Shared.Preferences;
using Robust.Shared.GameObjects;

namespace Content.Shared._Aquila.WOU.Switching;

[RegisterComponent]
public sealed partial class SwitchingHumanProfileComponent : Component
{
    [DataField] public HumanoidCharacterProfile Profile = new();
}