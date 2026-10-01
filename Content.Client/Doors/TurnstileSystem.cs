using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Doors;

/// <inheritdoc/>
public sealed partial class TurnstileSystem : SharedTurnstileSystem // Aquila Change
{
    [Dependency] private AppearanceSystem _appearance = default!; // Aquila Change
    [Dependency] private AnimationPlayerSystem _animation = default!; // Aquila Change

    private const string AnimationKey = "Turnstile";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TurnstileComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnAnimationCompleted(Entity<TurnstileComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey)
            return;

        _appearance.SetData(ent, TurnstileVisualLayers.Base, TurnstileStates.Idle); // Aquila Change
    }

    protected override void PlayAnimation(EntityUid uid, TurnstileStates state) // Aquila Change
    {
        // Aquila Change start
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
        // Aquila Change end
    }
}
