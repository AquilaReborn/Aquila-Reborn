using Content.Goobstation.Shared.Xenobiology.Components;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Xenobiology.Systems;

public sealed partial class EatCorpseSystem
{
    [Dependency] private IGameTiming _timing = default!;

    /// <summary>
    /// Прошла ли у NPC пауза после сорванной попытки, можно ли снова искать труп.
    /// </summary>
    public bool CanNpcTryEat(Entity<CorpseEaterComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return _timing.CurTime >= ent.Comp.NextNpcAttempt;
    }

    /// <summary>
    /// Тихая попытка NPC начать есть труп: без попапа о неудаче и с учётом паузы.
    /// </summary>
    public bool NpcTryEatCorpse(Entity<CorpseEaterComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!CanNpcTryEat(ent) || !CanEatCorpse(ent, target, ent.Comp))
            return false;

        return TryEatCorpse(ent, target, ent.Comp);
    }

    /// <summary>
    /// Запускает паузу перед следующей попыткой NPC после сорванного поедания.
    /// </summary>
    public void StartNpcRetryCooldown(Entity<CorpseEaterComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.NextNpcAttempt = _timing.CurTime + ent.Comp.NpcRetryDelay;
    }
}
