// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Server._Kakila.ShuttleLink;
using Content.Server.GameTicking;
using Content.Server.Shuttles.Components; // один из двух using (Server/Shared) для FTLComponent/ShuttleComponent окажется лишним — это нормально
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Shared._Kakila.CCVars;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems; // FTLState
using Content.Shared.Timing;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Kakila.Sot;

/// <summary>
///     Логика раунда SoT.
///     Prepare: шаттл уходит в настоящее FTL-пространство (карта гиперпространства ShuttleSystem)
///     и висит там в состоянии Travelling с очень большим временем прыжка.
///     Start: у обоих зависших шаттлов меняется цель на карту сражения и таймер обнуляется —
///     дальше ShuttleSystem сам проводит стадии Arriving/Arrived.
/// </summary>
public sealed class SotRoundSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly ShuttleLinkSystem _links = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    /// <summary>
    ///     Сколько секунд шаттл "летит" в гиперпространстве, пока ждёт начала битвы.
    ///     Если за это время не вызвать sotroundstart, он вернётся на исходную точку.
    /// </summary>
    private const float HoldTime = 3 * 60 * 60f;

    /// <summary>Шаттлы, удерживаемые в FTL этой системой (подготовленные, но ещё не запущенные в битву).</summary>
    private readonly HashSet<EntityUid> _held = new();

    public override void Initialize()
    {
        base.Initialize();

        // Широковещательная подписка (без компонента): ShuttleSystem уже подписан на ShuttleComponent + FTLStartedEvent,
        // а движок запрещает дублировать такие подписки. FTLStartedEvent рассылается и как broadcast.
        SubscribeLocalEvent<FTLStartedEvent>(OnFtlStarted);
    }

    /// <summary>
    ///     ShuttleSystem при входе в гиперпространство разгоняет шаттл до 20 м/с вперёд.
    ///     Для удерживаемых шаттлов гасим скорость, чтобы они не улетали за часы ожидания.
    /// </summary>
    private void OnFtlStarted(ref FTLStartedEvent args)
    {
        var uid = args.Entity;

        if (!_held.Contains(uid))
            return;

        if (!TryComp<PhysicsComponent>(uid, out var body))
            return;

        _physics.SetLinearVelocity(uid, Vector2.Zero, body: body);
        _physics.SetAngularVelocity(uid, 0f, body: body);
    }

    #region Preset check

    /// <summary>
    ///     Проверяет, разрешено ли использование команд при текущем пресете (CVar kakila.sot_allowed_presets).
    /// </summary>
    public bool CheckPreset(out string error)
    {
        error = string.Empty;

        var raw = _cfg.GetCVar(KakilaCVars.SotAllowedPresets);
        var allowed = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (allowed.Length == 0)
            return true;

        var current = _ticker.CurrentPreset?.ID;
        if (current != null && allowed.Contains(current, StringComparer.OrdinalIgnoreCase))
            return true;

        error = $"Команда недоступна при текущем пресете (\"{current ?? "нет"}\"). " +
                $"Разрешены: {string.Join(", ", allowed)}. " +
                $"Изменить: cvar {KakilaCVars.SotAllowedPresets.Name} \"...\" (пусто = любой пресет).";
        return false;
    }

    #endregion

    #region Prepare

    /// <summary>
    ///     Отправляет шаттл с часами <paramref name="hourglassId"/> вместе со всем, что на нём находится,
    ///     в FTL-пространство и удерживает его там до sotroundstart.
    /// </summary>
    public bool TryPrepare(string hourglassId, out string error)
    {
        if (!CheckPreset(out error))
            return false;

        if (!_links.TryGetShuttle(hourglassId, out var grid))
        {
            error = $"Шаттл с ID часов \"{hourglassId}\" не найден.";
            return false;
        }

        if (HasComp<FTLComponent>(grid))
        {
            error = "Шаттл уже находится в FTL (прыжок или перезарядка). Подождите или дождитесь окончания.";
            return false;
        }

        if (!TryComp<ShuttleComponent>(grid, out var shuttle))
        {
            error = "У грида нет ShuttleComponent — он не является шаттлом.";
            return false;
        }

        var xform = Transform(grid);
        _held.Add(grid);

        // Цель по умолчанию — исходная точка: если о шаттле забудут, по истечении HoldTime он вернётся домой.
        _shuttle.FTLToCoordinates(grid, shuttle, xform.Coordinates, xform.LocalRotation, hyperspaceTime: HoldTime);

        // FTLToCoordinates молча ничего не делает при неудаче (нет FTLDrive, не удалась настройка).
        if (!HasComp<FTLComponent>(grid))
        {
            _held.Remove(grid);
            error = "Не удалось запустить FTL (проверьте FTLDriveComponent на гриде и лог сервера).";
            return false;
        }

        return true;
    }

    #endregion

    #region Start

    /// <summary>
    ///     Отправляет оба подготовленных шаттла на карту сражения. Оба должны быть уже в гиперпространстве
    ///     (sotroundprepare), тогда прибывают одновременно. Сначала всё проверяется, потом что-то меняется.
    /// </summary>
    public bool TryStart(
        string hourglassId1,
        string hourglassId2,
        MapId battleMap,
        Vector2 position1,
        Vector2 position2,
        out string error)
    {
        if (!CheckPreset(out error))
            return false;

        if (hourglassId1 == hourglassId2)
        {
            error = "ID часов первого и второго корабля должны различаться.";
            return false;
        }

        if (battleMap == MapId.Nullspace || !_map.TryGetMap(battleMap, out var mapUid))
        {
            error = $"Карта с ID {battleMap} не существует.";
            return false;
        }

        if (!_links.TryGetShuttle(hourglassId1, out var grid1))
        {
            error = $"Шаттл с ID часов \"{hourglassId1}\" не найден.";
            return false;
        }

        if (!_links.TryGetShuttle(hourglassId2, out var grid2))
        {
            error = $"Шаттл с ID часов \"{hourglassId2}\" не найден.";
            return false;
        }

        if (grid1 == grid2)
        {
            error = "Оба ID указывают на один и тот же шаттл.";
            return false;
        }

        if (!TryGetHeld(grid1, hourglassId1, out var ftl1, out error) ||
            !TryGetHeld(grid2, hourglassId2, out var ftl2, out error))
        {
            return false;
        }

        Redirect(ftl1, mapUid.Value, position1);
        Redirect(ftl2, mapUid.Value, position2);

        _held.Remove(grid1);
        _held.Remove(grid2);
        return true;
    }

    private bool TryGetHeld(EntityUid grid, string id, out FTLComponent ftl, out string error)
    {
        ftl = default!;
        error = string.Empty;

        if (!_held.Contains(grid) || !TryComp<FTLComponent>(grid, out var comp))
        {
            error = $"Шаттл \"{id}\" не подготовлен. Сначала выполните sotroundprepare {id}.";
            return false;
        }

        if (comp.State == FTLState.Starting)
        {
            error = $"Шаттл \"{id}\" ещё уходит в гиперпространство. Подождите несколько секунд.";
            return false;
        }

        if (comp.State != FTLState.Travelling)
        {
            error = $"Шаттл \"{id}\" не находится в гиперпространстве (состояние: {comp.State}).";
            return false;
        }

        ftl = comp;
        return true;
    }

    /// <summary>
    ///     Меняет цель прыжка и завершает стадию Travelling прямо сейчас:
    ///     ShuttleSystem сам переведёт шаттл в Arriving, а затем поставит его в целевую точку.
    /// </summary>
    private void Redirect(FTLComponent ftl, EntityUid mapUid, Vector2 position)
    {
        ftl.TargetCoordinates = new EntityCoordinates(mapUid, position);
        ftl.TargetAngle = Angle.Zero;
        ftl.StateTime = StartEndTime.FromStartDuration(_timing.CurTime, TimeSpan.Zero);
    }

    #endregion
}
