// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Kakila.ShuttleLink;
using Content.Shared._Kakila.Sot;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Kakila.Sot;

/// <summary>
///     Колонка SoT. Включается и выключается взаимодействием. В начале битвы корабля, на котором она стоит,
///     включается сама, а если уже играла - перезапускает трек с начала.
/// </summary>
public sealed class SotSpeakerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ShuttleLinkSystem _links = default!;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextCheck;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SotSpeakerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<SotSpeakerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SotBattleStartedEvent>(OnBattleStarted);
    }

    /// <summary>Трек без зацикливания заканчивается сам: гасим индикатор.</summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextCheck)
            return;

        _nextCheck = _timing.CurTime + CheckInterval;

        var query = EntityQueryEnumerator<SotSpeakerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AudioStream == null || _audio.IsPlaying(comp.AudioStream))
                continue;

            comp.AudioStream = null;
            SetPlaying(uid, false);
        }
    }

    private void OnActivate(Entity<SotSpeakerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.AudioStream != null && _audio.IsPlaying(ent.Comp.AudioStream))
        {
            Stop(ent);
            _popup.PopupEntity(Loc.GetString("sot-speaker-popup-off"), ent, args.User);
        }
        else
        {
            Play(ent);
            _popup.PopupEntity(Loc.GetString("sot-speaker-popup-on"), ent, args.User);
        }
    }

    private void OnShutdown(Entity<SotSpeakerComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);
    }

    /// <summary>Битва началась: колонки на кораблях-участниках включаются, играющие перезапускаются.</summary>
    private void OnBattleStarted(SotBattleStartedEvent ev)
    {
        var grids = new HashSet<EntityUid>();
        if (_links.TryGetShuttle(ev.IdA, out var gridA))
            grids.Add(gridA);
        if (_links.TryGetShuttle(ev.IdB, out var gridB))
            grids.Add(gridB);

        if (grids.Count == 0)
            return;

        var query = EntityQueryEnumerator<SotSpeakerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!comp.PlayOnBattleStart || xform.GridUid is not { } grid || !grids.Contains(grid))
                continue;

            Play((uid, comp));
        }
    }

    /// <summary>Запускает трек с начала. Если колонка уже играла, прежний поток останавливается.</summary>
    private void Play(Entity<SotSpeakerComponent> ent)
    {
        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);

        var audioParams = AudioParams.Default
            .WithLoop(ent.Comp.Loop)
            .WithVolume(ent.Comp.Volume)
            .WithMaxDistance(ent.Comp.MaxDistance);

        ent.Comp.AudioStream = _audio.PlayPvs(ent.Comp.Sound, ent, audioParams)?.Entity;
        SetPlaying(ent, ent.Comp.AudioStream != null);
    }

    private void Stop(Entity<SotSpeakerComponent> ent)
    {
        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);
        SetPlaying(ent, false);
    }

    private void SetPlaying(EntityUid uid, bool playing)
    {
        _appearance.SetData(uid, SotSpeakerVisuals.Playing, playing);
    }
}
