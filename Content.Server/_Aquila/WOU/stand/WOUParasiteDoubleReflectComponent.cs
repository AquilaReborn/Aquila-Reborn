using Robust.Shared.GameObjects;

namespace Content.Server._Aquila.WOU.Stand;

[RegisterComponent]
public sealed partial class WOUParasiteDoubleReflectComponent : Component
{
    [DataField] public TimeSpan Duration = TimeSpan.FromSeconds(5);
    [DataField] public float ReflectMultiplier = 2f;

    [ViewVariables] public TimeSpan ExpiresAt;
}