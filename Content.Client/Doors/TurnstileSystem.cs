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
}
