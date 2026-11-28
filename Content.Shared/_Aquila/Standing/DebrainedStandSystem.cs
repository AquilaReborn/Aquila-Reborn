using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Standing;
using Content.Shared.Stunnable;

namespace Content.Shared._Aquila.Standing;

/// <summary>
/// Не даёт встать телу без мозга: отменяет попытку подняться и смену позы.
/// Работает и на клиенте, чтобы предсказание позы совпадало с сервером.
/// </summary>
public sealed class DebrainedStandSystem : EntitySystem
{
    /// <summary>
    /// Подписывается на попытки встать и подняться.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DebrainedComponent, StandAttemptEvent>(OnStandAttempt);
        SubscribeLocalEvent<DebrainedComponent, StandUpAttemptEvent>(OnStandUpAttempt);
    }

    private void OnStandAttempt(Entity<DebrainedComponent> ent, ref StandAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnStandUpAttempt(Entity<DebrainedComponent> ent, ref StandUpAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
