using Content.Server._Aquila.Heretic.Components;
using Content.Server._Aquila.Heretic.Events;
using Content.Server.Heretic.Components;
using Content.Shared._Aquila.Heretic.Events;
using Content.Shared.Heretic;
using Content.Shared.Mind;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Heretic.Systems;

/// <summary>
/// Даёт еретику полное зрение на время после появления сдвига реальности рядом с ним.
/// </summary>
public sealed partial class HereticRealityShiftVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityAppearedNearEvent>(OnEntityAppearedNear);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var expired = new ValueList<EntityUid>();

        var query = EntityQueryEnumerator<RealityShiftVisionComponent>();
        while (query.MoveNext(out var uid, out var vision))
        {
            if (vision.Active)
            {
                if (vision.EndTime > now)
                    continue;

                if (TryComp(uid, out EyeComponent? eye))
                    _eye.SetDrawFov(uid, vision.OriginalDrawFov, eye);

                vision.Active = false;
                vision.CooldownUntil = now + vision.CooldownDuration;
                continue;
            }

            if (vision.CooldownUntil > now)
                continue;

            expired.Add(uid);
        }

        foreach (var uid in expired)
        {
            RemComp<RealityShiftVisionComponent>(uid);
        }
    }

    private void OnEntityAppearedNear(ref EntityAppearedNearEvent args)
    {
        if (!HasComp<EldritchInfluenceComponent>(args.Entity))
            return;

        var player = args.Player;

        if (!_mind.TryGetMind(player, out var mindId, out _) || !HasComp<HereticComponent>(mindId))
            return;

        if (!TryComp(player, out EyeComponent? eye))
            return;

        var now = _timing.CurTime;

        if (!TryComp<RealityShiftVisionComponent>(player, out var vision))
        {
            vision = EnsureComp<RealityShiftVisionComponent>(player);
        }
        else if (vision.Active)
        {
            return;
        }
        else if (vision.CooldownUntil > now)
        {
            return;
        }

        vision.OriginalDrawFov = eye.DrawFov;
        vision.Active = true;
        vision.EndTime = now + vision.VisionDuration;
        _eye.SetDrawFov(player, false, eye);
        if (TryComp<ActorComponent>(player, out var actor))
            _audio.PlayGlobal(vision.TriggerSound, Filter.SinglePlayer(actor.PlayerSession), true);
        RaiseNetworkEvent(new RealityShiftVisionStartedEvent { Duration = vision.VisionDuration }, player);
    }
}
