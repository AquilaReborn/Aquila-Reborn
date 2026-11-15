using Content.Server.Radio;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server._Aquila.Chat;

public sealed partial class MobRadioMuffleSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    /// <summary>
    /// Отключает приём радио для мобов в критическом состоянии и мёртвых.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
    }

    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent args)
    {
        var listener = args.RadioReceiver;
        if (!HasComp<MobStateComponent>(listener))
            listener = Transform(listener).ParentUid;

        if (!HasComp<MobStateComponent>(listener))
            return;

        if (!_mobState.IsCritical(listener) && !_mobState.IsDead(listener))
            return;

        args.Cancelled = true;
    }
}
