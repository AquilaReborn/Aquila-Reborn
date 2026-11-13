// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Station.Systems;
using Content.Shared._Aquila.EventContent.Atlas739;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.DeviceLinking;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.EventContent.Atlas739;

/// <summary>
///     Автоматически связывает консоли закупок SoT с телепадами закупок ТОЙ ЖЕ СТОРОНЫ на том же гриде
///     (порт консоли OrderSender -> порт телепада OrderReceiver, как это делает сетевой конфигуратор).
///     Заказ материализуется только на телепаде, связанном с консолью, поэтому без связи магазин не работает.
///     Для каждого корабля это одна консоль и один или несколько телепадов своей стороны на его гриде.
///
///     Сторона (SotShopConsole.Side / SotShopTelepad.Side) проверяется, чтобы консоль одной фракции не
///     связалась с телепадом другой, если обе стороны стоят на одном гриде (например, общая тестовая
///     станция): без этой проверки заказ мог прийти не на тот телепад, а раз телепад принимает заказы
///     только от того, с кем он связан, orderDispatch не находил "свой" телепад и откатывался на обычную
///     доставку станции (грузовые платформы / трейдпост), если они есть на карте.
///     Не ставьте на одном гриде две консоли ОДНОЙ стороны с общими телепадами: телепад принимает заказы
///     только от первого связанного источника, и для какой из консолей он будет работать - не определено.
///
///     Дополнительно система добавляет рынок и счёт консоли в базу станции, на которой она стоит:
///       - рынок (allowedGroups) - в StationCargoOrderDatabase.Markets, иначе каталог консоли пуст;
///       - счёт (Account) - в StationBankAccount.Accounts и StationCargoOrderDatabase.Orders, иначе первый
///         же заказ упадёт с ошибкой (на станции просто нет такого счёта). Так консоли SoT работают на
///         любой станции, а не только на выделенных SotStationAthena / SotStationReapers.
/// </summary>
public sealed class SotShopLinkSystem : EntitySystem
{
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedCargoSystem _cargo = default!;

    private static readonly TimeSpan StationCheckInterval = TimeSpan.FromSeconds(3);
    private TimeSpan _nextStationCheck;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SotShopConsoleComponent, MapInitEvent>(OnConsoleInit);
        SubscribeLocalEvent<SotShopTelepadComponent, MapInitEvent>(OnTelepadInit);
    }

    /// <summary>
    ///     Станция появляется уже после загрузки карты, поэтому рынок и счёт добавляются не один раз
    ///     при старте, а периодически (дёшево: консолей закупок единицы).
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextStationCheck)
            return;

        _nextStationCheck = _timing.CurTime + StationCheckInterval;

        var query = EntityQueryEnumerator<SotShopConsoleComponent, CargoOrderConsoleComponent>();
        while (query.MoveNext(out var uid, out _, out var console))
        {
            EnsureStationSetup(uid, console);
        }
    }

    /// <summary>
    ///     Добавляет рынок и счёт консоли в базу станции, на которой она стоит, если их там ещё нет.
    /// </summary>
    private void EnsureStationSetup(EntityUid uid, CargoOrderConsoleComponent console)
    {
        if (_station.GetOwningStation(uid) is not { } station)
            return;

        if (TryComp<StationCargoOrderDatabaseComponent>(station, out var db))
        {
            foreach (var market in console.AllowedGroups)
            {
                if (!db.Markets.Contains(market))
                    db.Markets.Add(market);
            }

            // Orders[Account] читается прямым индексатором (без TryGetValue), поэтому ключ обязан
            // существовать до первого заказа с этого счёта.
            if (!db.Orders.ContainsKey(console.Account))
                db.Orders[console.Account] = new List<CargoOrderData>();
        }

        // Accounts закрыт атрибутом Access(typeof(SharedCargoSystem)) (RA0002), поэтому и чтение,
        // и создание счёта идут через публичный API SharedCargoSystem.
        if (TryComp<StationBankAccountComponent>(station, out var bank)
            && !_cargo.TryGetAccount((station, bank), console.Account, out _))
        {
            _cargo.TrySetBankAccount((station, bank), console.Account, 0, createAccount: true);
        }
    }

    private void OnConsoleInit(Entity<SotShopConsoleComponent> ent, ref MapInitEvent args)
    {
        if (TryComp<CargoOrderConsoleComponent>(ent, out var console))
            EnsureStationSetup(ent, console);

        LinkGrid(Transform(ent).GridUid);
    }

    private void OnTelepadInit(Entity<SotShopTelepadComponent> ent, ref MapInitEvent args)
    {
        LinkGrid(Transform(ent).GridUid);
    }

    /// <summary>Связывает консоли закупок грида с телепадами закупок той же стороны на этом же гриде.</summary>
    private void LinkGrid(EntityUid? grid)
    {
        if (grid == null)
            return;

        var consoles = new List<(EntityUid Uid, SotSide Side)>();
        var consoleQuery = EntityQueryEnumerator<SotShopConsoleComponent, TransformComponent>();
        while (consoleQuery.MoveNext(out var uid, out var comp, out var xform))
        {
            if (xform.GridUid == grid)
                consoles.Add((uid, comp.Side));
        }

        if (consoles.Count == 0)
            return;

        var padQuery = EntityQueryEnumerator<SotShopTelepadComponent, DeviceLinkSinkComponent, TransformComponent>();
        while (padQuery.MoveNext(out var pad, out var padComp, out var sink, out var padXform))
        {
            if (padXform.GridUid != grid)
                continue;

            foreach (var (console, side) in consoles)
            {
                if (side != padComp.Side)
                    continue;

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
