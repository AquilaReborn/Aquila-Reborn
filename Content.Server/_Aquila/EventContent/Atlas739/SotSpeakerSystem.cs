// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Aquila.EventContent.Atlas739;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.EventContent.Atlas739;

/// <summary>
///     Колонка SoT, серверная часть. Сервер не проигрывает звук сам: он хранит состояние колонки
///     (играет ли, когда запущен трек), а клиенты игроков, стоящих на том же шаттле, проигрывают музыку
///     локально и равномерно (см. клиентскую SotSpeakerClientSystem).
///     Включается по взаимодействию, а в начале битвы своего корабля включается сама (с начала трека).
/// </summary>
public sealed class SotSpeakerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ShuttleLinkSystem _links = default!;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextCheck;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SotSpeakerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SotSpeakerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SotSpeakerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<SotBattleStartedEvent>(OnBattleStarted);
    }

    /// <summary>
    ///     Колонка должна быть известна клиентам, даже если она далеко от игрока (большой корабль),
    ///     иначе клиент не узнает, что музыку нужно играть.
    /// </summary>
    private void OnMapInit(Entity<SotSpeakerComponent> ent, ref MapInitEvent args)
    {
        _pvs.AddGlobalOverride(ent);
    }

    private void OnShutdown(Entity<SotSpeakerComponent> ent, ref ComponentShutdown args)
    {
        _pvs.RemoveGlobalOverride(ent);
    }

    /// <summary>Трек без зацикливания играет один раз: по окончании колонка выключается.</summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextCheck)
            return;

        _nextCheck = _timing.CurTime + CheckInterval;

        var query = EntityQueryEnumerator<SotSpeakerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Playing && comp.EndsAt is { } endsAt && _timing.CurTime >= endsAt)
                Stop((uid, comp));
        }
    }

    private void OnActivate(Entity<SotSpeakerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.Playing)
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

    /// <summary>
    ///     Запускает трек с начала. Если колонка уже играла, StartTime обновляется,
    ///     и клиенты перезапускают трек.
    /// </summary>
    private void Play(Entity<SotSpeakerComponent> ent)
    {
        var comp = ent.Comp;

        if (comp.Sound is not SoundPathSpecifier path)
        {
            Log.Error($"Колонка {ToPrettyString(ent)}: поддерживается только путь к файлу (sound.path), не набор звуков.");
            return;
        }

        if (!path.Path.ToString().StartsWith('/'))
        {
            Log.Error($"Колонка {ToPrettyString(ent)}: путь к треку должен начинаться с / (например, /Audio/...), сейчас: {path.Path}");
            return;
        }

        comp.EndsAt = null;
        if (!comp.Loop)
        {
            try
            {
                comp.EndsAt = _timing.CurTime + _audio.GetAudioLength(path.Path.ToString());
            }
            catch (Exception e)
            {
                // Нет файла или не удалось прочитать длину: выключится вручную.
                Log.Error($"Колонка {ToPrettyString(ent)}: не удалось получить длину трека {path.Path}: {e.Message}");
            }
        }

        comp.Playing = true;
        comp.StartTime = _timing.CurTime;
        Dirty(ent);
        SetPlaying(ent, true);
    }

    private void Stop(Entity<SotSpeakerComponent> ent)
    {
        ent.Comp.Playing = false;
        ent.Comp.EndsAt = null;
        Dirty(ent);
        SetPlaying(ent, false);
    }

    private void SetPlaying(EntityUid uid, bool playing)
    {
        _appearance.SetData(uid, SotSpeakerVisuals.Playing, playing);
    }
}
