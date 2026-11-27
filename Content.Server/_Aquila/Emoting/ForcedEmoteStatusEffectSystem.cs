using Content.Server.Chat.Systems;
using Content.Shared._Aquila.Emoting;
using Content.Shared.Chat;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Emoting;

/// <summary>
/// Заставляет существ с <see cref="ForcedEmoteStatusEffectComponent"/> выполнять эмоцию, которую им навязывает статус-эффект.
/// </summary>
public sealed class ForcedEmoteStatusEffectSystem : SharedForcedEmoteStatusEffectSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForcedEmoteStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ForcedEmoteStatusEffectComponent, StatusEffectComponent>();
        while (query.MoveNext(out _, out var forced, out var status))
        {
            if (status.AppliedTo is not { } target || now < forced.NextEmote)
                continue;

            forced.NextEmote = now + forced.Interval;
            ForceEmote(target, forced);
        }
    }

    private void OnApplied(Entity<ForcedEmoteStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.NextEmote = _timing.CurTime + ent.Comp.Interval;
        ForceEmote(args.Target, ent.Comp);
    }

    private void ForceEmote(EntityUid target, ForcedEmoteStatusEffectComponent forced)
    {
        if (forced.ShowInChat)
        {
            _chat.TryEmoteWithChat(target,
                forced.Emote,
                ChatTransmitRange.GhostRangeLimit,
                ignoreActionBlocker: true,
                forceEmote: true);
            return;
        }

        _chat.TryEmoteWithoutChat(target, forced.Emote, ignoreActionBlocker: true);
    }
}
