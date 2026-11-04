// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Aquila.EventContent.Atlas739;
using Content.Shared.Actions;
using Content.Shared.Administration.Systems;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.EventContent.Atlas739;

/// <summary>
///     Платформы воскрешения на базе. Привязка: клик по платформе (повторный клик отвязывает).
///
///     Смерть привязанного игрока запоминается (DiedAt). Дальше два режима (SotRespawnPad.AutoRespawn):
///       - false (по умолчанию): когда игрок становится призраком, он получает действие "Воскреснуть на платформе".
///         Оно становится доступным через ActionDelay (10 с) после смерти и воскрешает ЗА ОДИН ШАГ: тело переносится
///         на платформу, полностью лечится, душа возвращается в тело;
///       - true: воскрешение срабатывает само через Delay после смерти.
///     Если тело уничтожено (разорвано), воскрешать нечего: привязка пропадает вместе с ним.
///     Гиб игрокам с SotFaction запрещён отдельной системой SotFactionSystem.
/// </summary>
public sealed class SotRespawnSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(0.5);
    private TimeSpan _nextCheck;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SotRespawnPadComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<SotRespawnPadComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SotRespawnPadComponent, ComponentShutdown>(OnPadShutdown);
        SubscribeLocalEvent<SotRespawnBoundComponent, ComponentShutdown>(OnBoundShutdown);
        SubscribeLocalEvent<SotRespawnBoundComponent, MobStateChangedEvent>(OnMobStateChanged);

        // Действие призрака. Подписка на GhostComponent: событие приходит на призрака (исполнителя).
        SubscribeLocalEvent<GhostComponent, SotRespawnActionEvent>(OnRespawnAction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (now < _nextCheck)
            return;

        _nextCheck = now + CheckInterval;

        var query = EntityQueryEnumerator<SotRespawnBoundComponent>();
        while (query.MoveNext(out var uid, out var bound))
        {
            // Автоматическое воскрешение.
            if (bound.RespawnAt is { } at && now >= at)
            {
                Respawn(uid, bound);
                continue;
            }

            // Выдача действия призраку: игрок мёртв, стал призраком-посетителем, действия у него ещё нет.
            if (bound.DiedAt != null && bound.GhostAction == null)
                TryGrantAction(uid, bound);
        }
    }

    #region Привязка

    private void OnActivate(Entity<SotRespawnPadComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var user = args.User;
        var pad = ent.Comp;

        // Платформа фракции: привязаться могут только её члены (компонент SotFaction).
        if (pad.Side is { } side
            && !(TryComp<SotFactionComponent>(user, out var faction) && faction.Side == side))
        {
            _popup.PopupEntity(Loc.GetString("sot-respawn-popup-wrong-faction"), ent, user);
            return;
        }

        // Повторный клик отвязывает.
        if (pad.Bound.Contains(user))
        {
            RemComp<SotRespawnBoundComponent>(user);
            _popup.PopupEntity(Loc.GetString("sot-respawn-popup-unbound"), ent, user);
            return;
        }

        if (pad.Bound.Count >= pad.MaxBound)
        {
            _popup.PopupEntity(Loc.GetString("sot-respawn-popup-full"), ent, user);
            return;
        }

        // Привязан к другой платформе: перепривязываем сюда.
        if (TryComp<SotRespawnBoundComponent>(user, out var old) && old.Pad != ent.Owner)
            RemComp<SotRespawnBoundComponent>(user);

        var bound = EnsureComp<SotRespawnBoundComponent>(user);
        bound.Pad = ent;
        pad.Bound.Add(user);
        UpdateVisual(ent);

        _popup.PopupEntity(Loc.GetString("sot-respawn-popup-bound"), ent, user);
    }

    private void OnExamined(Entity<SotRespawnPadComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("sot-respawn-examine",
            ("count", ent.Comp.Bound.Count),
            ("max", ent.Comp.MaxBound)));

        foreach (var bound in ent.Comp.Bound)
        {
            args.PushMarkup(Loc.GetString("sot-respawn-examine-bound", ("name", Name(bound))));
        }
    }

    /// <summary>Платформа удалена: привязки снимаются.</summary>
    private void OnPadShutdown(Entity<SotRespawnPadComponent> ent, ref ComponentShutdown args)
    {
        foreach (var bound in ent.Comp.Bound)
        {
            RemCompDeferred<SotRespawnBoundComponent>(bound);
        }
    }

    /// <summary>Игрок отвязан или удалён: убираем его из списка платформы и забираем действие у призрака.</summary>
    private void OnBoundShutdown(Entity<SotRespawnBoundComponent> ent, ref ComponentShutdown args)
    {
        RemoveGhostAction(ent.Comp);

        if (!TryComp<SotRespawnPadComponent>(ent.Comp.Pad, out var pad))
            return;

        pad.Bound.Remove(ent);
        UpdateVisual((ent.Comp.Pad, pad));
    }

    private void UpdateVisual(Entity<SotRespawnPadComponent> pad)
    {
        _appearance.SetData(pad, SotRespawnPadVisuals.Bound, pad.Comp.Bound.Count > 0);
    }

    #endregion

    #region Смерть и воскрешение

    private void OnMobStateChanged(Entity<SotRespawnBoundComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            ent.Comp.DiedAt = _timing.CurTime;

            // Автоматический режим: воскрешение само через Delay.
            if (TryComp<SotRespawnPadComponent>(ent.Comp.Pad, out var pad) && pad.AutoRespawn)
                ent.Comp.RespawnAt = _timing.CurTime + pad.Delay;

            return;
        }

        // Игрока вылечили раньше (дефибриллятор и т. п.): воскрешение платформой не нужно.
        ent.Comp.RespawnAt = null;
        ent.Comp.DiedAt = null;
        RemoveGhostAction(ent.Comp);
    }

    /// <summary>
    ///     Выдаёт призраку привязанного игрока действие воскрешения. Игрок должен быть мёртв, а его душа - в роли
    ///     призрака-посетителя (может вернуться). Действие недоступно до DiedAt + ActionDelay (кулдаун).
    /// </summary>
    private void TryGrantAction(EntityUid body, SotRespawnBoundComponent bound)
    {
        if (!TryComp<SotRespawnPadComponent>(bound.Pad, out var pad)
            || !_mind.TryGetMind(body, out _, out var mind)
            || mind.OwnedEntity != body
            || mind.VisitingEntity is not { } ghost
            || !HasComp<GhostComponent>(ghost))
        {
            return;
        }

        EntityUid? action = null;
        if (!_actions.AddAction(ghost, ref action, pad.ActionPrototype) || action == null)
            return;

        bound.Ghost = ghost;
        bound.GhostAction = action;

        // Задержка считается от смерти: если призрак появился позже, часть времени уже прошла.
        var now = _timing.CurTime;
        var availableAt = bound.DiedAt!.Value + pad.ActionDelay;
        if (availableAt > now)
            _actions.SetCooldown(action, now, availableAt);
    }

    private void RemoveGhostAction(SotRespawnBoundComponent bound)
    {
        if (bound.Ghost is { } ghost && bound.GhostAction is { } action && !TerminatingOrDeleted(ghost))
            _actions.RemoveAction(ghost, action);

        bound.Ghost = null;
        bound.GhostAction = null;
    }

    /// <summary>Нажатие действия "Воскреснуть на платформе" призраком.</summary>
    private void OnRespawnAction(EntityUid uid, GhostComponent component, SotRespawnActionEvent args)
    {
        if (args.Handled)
            return;

        // Тело игрока берём из его разума: призрак - лишь посетитель, владелец разума - тело.
        if (!_mind.TryGetMind(uid, out _, out var mind)
            || mind.OwnedEntity is not { } body
            || !TryComp<SotRespawnBoundComponent>(body, out var bound)
            || !TryComp<SotRespawnPadComponent>(bound.Pad, out var pad))
        {
            _popup.PopupEntity(Loc.GetString("sot-respawn-popup-action-no-body"), uid, uid);
            return;
        }

        // Защита от обхода кулдауна: проверяем время ещё раз на сервере.
        if (bound.DiedAt is { } diedAt && _timing.CurTime < diedAt + pad.ActionDelay)
        {
            var left = (int) Math.Ceiling((diedAt + pad.ActionDelay - _timing.CurTime).TotalSeconds);
            _popup.PopupEntity(Loc.GetString("sot-respawn-popup-action-wait", ("seconds", left)), uid, uid);
            return;
        }

        args.Handled = true;
        Respawn(body, bound);
    }

    /// <summary>Телепортирует тело на платформу, лечит его и возвращает душу. Сразу, без задержек.</summary>
    private void Respawn(EntityUid uid, SotRespawnBoundComponent bound)
    {
        bound.RespawnAt = null;

        if (TerminatingOrDeleted(bound.Pad) || !TryComp<SotRespawnPadComponent>(bound.Pad, out var pad))
        {
            RemCompDeferred<SotRespawnBoundComponent>(uid);
            return;
        }

        if (!_mobState.IsDead(uid))
            return;

        var origin = Transform(uid).Coordinates;
        var target = Transform(bound.Pad).Coordinates;

        // Освобождаем тело: буксировка и контейнеры (шкаф, мешок для трупов) телепорту мешают.
        if (TryComp<PullableComponent>(uid, out var pullable) && pullable.BeingPulled)
            _pulling.TryStopPull(uid, pullable, ignoreGrab: true);

        if (TryComp<PullerComponent>(uid, out var puller)
            && puller.Pulling is { } pulledUid
            && TryComp<PullableComponent>(pulledUid, out var pulled))
        {
            _pulling.TryStopPull(pulledUid, pulled, ignoreGrab: true);
        }

        if (_container.IsEntityInContainer(uid))
            _container.TryRemoveFromContainer(uid);

        _audio.PlayPvs(pad.DepartureSound, origin);
        _transform.SetCoordinates(uid, target);

        // Полное лечение: урон, состояние (Dead -> Alive), кровотечение и т. д.
        _rejuvenate.PerformRejuvenate(uid);

        // Возвращаем душу: призрак-посетитель исчезает, игрок снова в теле.
        ReturnMind(uid);

        // Действие призрака больше не нужно (сам призрак исчезнет вместе с возвратом души).
        RemoveGhostAction(bound);
        bound.DiedAt = null;

        _audio.PlayPvs(pad.ArrivalSound, uid);
        _popup.PopupEntity(Loc.GetString("sot-respawn-popup-revived"), uid, uid, PopupType.Large);
    }

    /// <summary>
    ///     Если игрок после смерти превратился в призрака-посетителя (может вернуться), возвращаем его в тело.
    ///     Тем, кто окончательно покинул тело, воскрешение платформой душу не вернёт.
    /// </summary>
    private void ReturnMind(EntityUid body)
    {
        if (!_mind.TryGetMind(body, out var mindId, out var mind))
            return;

        if (mind.OwnedEntity == body && mind.VisitingEntity != null)
            _mind.UnVisit(mindId, mind);
    }

    #endregion
}
