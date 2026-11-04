// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Aquila.EventContent.Atlas739;
using Content.Shared.CCVar;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._Aquila.EventContent.Atlas739;

/// <summary>
///     Музыка колонок SoT, клиентская часть. Для игрока, стоящего на том же гриде, что и играющая колонка,
///     трек проигрывается локально как глобальный звук: одинаково громко по всему шаттлу,
///     без затухания с расстоянием и без заглушения стенами. Стоит игроку сойти с шаттла,
///     музыка выключается; вернётся он, и трек продолжится с нужного места (позиция считается
///     по времени запуска, записанному сервером). Громкость подчиняется ползунку музыки в настройках.
/// </summary>
public sealed class SotSpeakerClientSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private sealed class Entry
    {
        /// <summary>StartTime колонки, для которого запущен этот поток.</summary>
        public TimeSpan StartTime;

        /// <summary>Поток звука. null - трек без зацикливания уже доиграл, заново не запускаем.</summary>
        public EntityUid? Stream;
    }

    private readonly Dictionary<EntityUid, Entry> _entries = new();
    private readonly List<EntityUid> _toRemove = new();

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(0.25);
    private TimeSpan _nextCheck;

    private float _volumeSlider;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, CCVars.AmbientMusicVolume, OnVolumeChanged, true);

        SubscribeLocalEvent<SotSpeakerComponent, ComponentShutdown>(OnSpeakerShutdown);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        StopAll();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted || _timing.CurTime < _nextCheck)
            return;

        _nextCheck = _timing.CurTime + CheckInterval;

        // Грид, на котором стоит игрок. Если игрока нет или он не на гриде, музыки нет.
        EntityUid? localGrid = _player.LocalEntity is { } local ? Transform(local).GridUid : null;

        var query = EntityQueryEnumerator<SotSpeakerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            var shouldPlay = comp.Playing
                             && localGrid != null
                             && xform.GridUid == localGrid;

            if (!shouldPlay)
            {
                Stop(uid);
                continue;
            }

            // Уже играет (или доиграл) именно этот запуск трека.
            if (_entries.TryGetValue(uid, out var entry)
                && entry.StartTime == comp.StartTime
                && (entry.Stream == null || Exists(entry.Stream)))
            {
                continue;
            }

            Start(uid, comp);
        }

        // Колонки, исчезнувшие без события завершения.
        _toRemove.Clear();
        foreach (var uid in _entries.Keys)
        {
            if (!Exists(uid) || !HasComp<SotSpeakerComponent>(uid))
                _toRemove.Add(uid);
        }

        foreach (var uid in _toRemove)
            Stop(uid);
    }

    private void Start(EntityUid uid, SotSpeakerComponent comp)
    {
        Stop(uid);

        if (comp.Sound is not SoundPathSpecifier path)
            return;

        // Путь в прототипе должен быть от корня ресурсов: /Audio/... (первый слэш обязателен).
        if (!path.Path.ToString().StartsWith('/'))
        {
            Log.Error($"Колонка SoT: путь к треку должен начинаться с / (например, /Audio/...), сейчас: {path.Path}");
            _entries[uid] = new Entry { StartTime = comp.StartTime, Stream = null };
            return;
        }

        var elapsed = _timing.CurTime - comp.StartTime;
        if (elapsed < TimeSpan.Zero)
            elapsed = TimeSpan.Zero;

        double offset;
        try
        {
            var length = _audio.GetAudioLength(path.Path.ToString()).TotalSeconds;

            if (comp.Loop)
            {
                offset = length > 0.1 ? elapsed.TotalSeconds % length : 0;
            }
            else if (elapsed.TotalSeconds >= length)
            {
                // Трек уже закончился: запоминаем, чтобы не запускать заново.
                _entries[uid] = new Entry { StartTime = comp.StartTime, Stream = null };
                return;
            }
            else
            {
                offset = elapsed.TotalSeconds;
            }
        }
        catch (Exception e)
        {
            Log.Error($"Колонка SoT: не удалось получить длину трека {path.Path}: {e.Message}");
            return;
        }

        var audioParams = AudioParams.Default
            .WithLoop(comp.Loop)
            .WithVolume(comp.Volume + _volumeSlider)
            .WithPlayOffset((float) offset);

        var stream = _audio.PlayGlobal(comp.Sound, Filter.Local(), false, audioParams)?.Entity;
        _entries[uid] = new Entry { StartTime = comp.StartTime, Stream = stream };
    }

    private void Stop(EntityUid uid)
    {
        if (_entries.Remove(uid, out var entry) && entry.Stream is { } stream)
            _audio.Stop(stream);
    }

    private void StopAll()
    {
        foreach (var entry in _entries.Values)
        {
            if (entry.Stream is { } stream)
                _audio.Stop(stream);
        }

        _entries.Clear();
    }

    private void OnSpeakerShutdown(Entity<SotSpeakerComponent> ent, ref ComponentShutdown args)
    {
        Stop(ent);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        StopAll();
    }

    private void OnVolumeChanged(float value)
    {
        _volumeSlider = SharedAudioSystem.GainToVolume(value);

        foreach (var (uid, entry) in _entries)
        {
            if (entry.Stream is not { } stream || !TryComp<SotSpeakerComponent>(uid, out var comp))
                continue;

            _audio.SetVolume(stream, comp.Volume + _volumeSlider);
        }
    }
}
