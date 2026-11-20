using Robust.Shared.GameObjects;

namespace Content.Server._Aquila.WOU.Stand;

[RegisterComponent]
public sealed partial class WOUParasiteDoubleReflectComponent : Component
{
    [ViewVariables] public TimeSpan ExpiresAt;
}