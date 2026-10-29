using Content.Shared.Doors.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Doors;

public sealed partial class TurnstileSystem
{
    protected override void PlayAnimation(EntityUid uid, TurnstileStates state)
    {
        if (!TryComp<AnimationPlayerComponent>(uid, out var animation))
            return;

        if (_animation.HasRunningAnimation(uid, AnimationKey) || !TryComp<TurnstileComponent>(uid, out var turnComp))
            return;

        var anim = new Animation
        {
            Length = turnComp.AnimationCooldown,
        };

        _animation.Play((uid, animation), anim, AnimationKey);
        _appearance.SetData(uid, TurnstileVisualLayers.Base, state);
    }
}
