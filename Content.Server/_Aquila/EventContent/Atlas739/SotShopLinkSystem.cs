// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Station.Systems;
using Content.Shared._Aquila.EventContent.Atlas739;
using Content.Shared.Cargo.Components;
using Content.Shared.DeviceLinking;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.EventContent.Atlas739;

/// <summary>
///     Автоматически связывает консоли закупок SoT с телепадами закупок на том же гриде
///     (порт консоли OrderSender -> порт телепада OrderReceiver, как это делает сетевой конфигуратор).
///     Заказ материализуется только на телепаде, связанном с консолью, поэтому без связи магазин не работает.
///     Для каждого корабля это одна консоль и один или несколько телепадов на его гриде.
///
///     Дополнительно система добавляет рынки консоли (allowedGroups) в список рынков станции, на которой она стоит.
///     Консоль показывает только товары рынков, которые есть и у неё, и у станции; у обычной станции
///     рынок один ("market"), поэтому без этого на ней консоль была бы пустой.
/// </summary>
public sealed class SotShopLinkSystem : EntitySystem
{
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StationSystem _station = default!;

    private static readonly TimeSpan MarketCheckInterval = TimeSpan.FromSeconds(3);
    private TimeSpan _nextMarketCheck;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SotShopConsoleComponent, MapInitEvent>(OnConsoleInit);
        SubscribeLocalEvent<SotShopTelepadComponent, MapInitEvent>(OnTelepadInit);
    }

    /// <summary>
    ///     Станция появляется уже после загрузки карты, поэтому рынки добавляются не один раз при старте,
    ///     а периодически (дёшево: консолей закупок единицы).
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextMarketCheck)
            return;

        _nextMarketCheck = _timing.CurTime + MarketCheckInterval;

        var query = EntityQueryEnumerator<SotShopConsoleComponent, CargoOrderConsoleComponent>();
        while (query.MoveNext(out var uid, out _, out var console))
        {
            EnsureMarkets(uid, console);
        }
    }

    /// <summary>Добавляет рынки консоли в базу заказов станции, если их там ещё нет.</summary>
    private void EnsureMarkets(EntityUid uid, CargoOrderConsoleComponent console)
    {
        if (_station.GetOwningStation(uid) is not { } station
            || !TryComp<StationCargoOrderDatabaseComponent>(station, out var db))
        {
            return;
        }

        foreach (var market in console.AllowedGroups)
        {
            if (!db.Markets.Contains(market))
                db.Markets.Add(market);
        }
    }

    private void OnConsoleInit(Entity<SotShopConsoleComponent> ent, ref MapInitEvent args)
    {
        if (TryComp<CargoOrderConsoleComponent>(ent, out var console))
            EnsureMarkets(ent, console);

        LinkGrid(Transform(ent).GridUid);
    }

    private void OnTelepadInit(Entity<SotShopTelepadComponent> ent, ref MapInitEvent args)
    {
        LinkGrid(Transform(ent).GridUid);
    }

    /// <summary>Связывает все консоли закупок грида со всеми телепадами закупок этого же грида.</summary>
    private void LinkGrid(EntityUid? grid)
    {
        if (grid == null)
            return;

        var consoles = new List<EntityUid>();
        var consoleQuery = EntityQueryEnumerator<SotShopConsoleComponent, TransformComponent>();
        while (consoleQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.GridUid == grid)
                consoles.Add(uid);
        }

        if (consoles.Count == 0)
            return;

        var padQuery = EntityQueryEnumerator<SotShopTelepadComponent, DeviceLinkSinkComponent, TransformComponent>();
        while (padQuery.MoveNext(out var pad, out _, out var sink, out var padXform))
        {
            if (padXform.GridUid != grid)
                continue;

            foreach (var console in consoles)
            {
                // Телепад принимает заказы от первого связанного источника: уже связанные не трогаем.
                // У LinkedSources закрыт доступ на вызов методов (RA0002), поэтому проверяем через LINQ:
                // поле здесь только читается и передаётся как аргумент.
                if (Enumerable.Contains(sink.LinkedSources, console))
                    continue;

                _deviceLink.LinkDefaults(null, console, pad);
            }
        }
    }
}
