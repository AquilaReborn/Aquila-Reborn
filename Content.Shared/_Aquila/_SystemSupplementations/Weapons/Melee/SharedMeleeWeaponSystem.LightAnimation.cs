using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Melee;

public abstract partial class SharedMeleeWeaponSystem
{
    private static (EntProtoId Animation, Angle Rotation) GetLightAttackAnimation(
        MeleeWeaponComponent component,
        EntityUid user,
        EntityUid weapon)
    {
        return weapon == user
            ? (component.Animation, component.AnimationRotation)
            : (component.WideAnimation, component.WideAnimationRotation);
    }
}
