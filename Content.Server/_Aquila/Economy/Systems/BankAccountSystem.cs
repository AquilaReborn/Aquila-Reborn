using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._Aquila.Economy.Components;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared._Aquila.CCVars;
using Content.Shared._Aquila.Economy;
using Content.Shared._Aquila.Economy.DataDefinitions;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Economy.Systems;

public sealed class BankAccountSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedCargoSystem _cargo = default!;
    [Dependency] private readonly StationBudgetLogSystem _budgetLog = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;
    [Dependency] private readonly StationSystem _station = default!;

    private const int MinPin = 1000;
    private const int MaxPinExclusive = 10000;

    private readonly Dictionary<EntityUid, PendingAccount> _pending = new();

    private readonly record struct PendingAccount(ProtoId<JobPrototype>? Job, string Name, TimeSpan Expiry);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<BankCardComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<IdCardComponent, ComponentStartup>(OnIdCardStartup);
    }

    private void OnIdCardStartup(Entity<IdCardComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.JobPrototype is not { } job ||
            !TryComp<BankCardComponent>(ent, out var card) ||
            card.Account is not { } accountUid ||
            !TryComp<BankAccountComponent>(accountUid, out var account))
            return;

        account.Job = job;
        account.Salary = ResolveSalary(account, job);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pending.Count > 0)
            UpdatePendingAccounts();

        var enabled = _cfg.GetCVar(AquilaCVars.Economy);
        var query = EntityQueryEnumerator<BankAccountComponent>();
        while (query.MoveNext(out var uid, out var account))
        {
            if (_timing.CurTime < account.NextPayday)
                continue;

            account.NextPayday += account.PaydayInterval;
            if (enabled)
                PaySalary((uid, account));
        }
    }

    private void PaySalary(Entity<BankAccountComponent> account)
    {
        var salary = account.Comp.Salary;
        if (salary <= 0)
            return;

        if (account.Comp.Station is { } station
            && TryComp<StationTreasuryComponent>(station, out var treasury)
            && treasury.PaySalaryFromTreasury)
        {
            if (!TryPayFromTreasury(station, salary, account.Comp.SalaryReason, treasury.CoverSalaryWithSubsidy, null, out var paid, account.AsNullable())
                && paid == 0)
            {
                Log.Warning($"Failed to pay salary of {salary} credits to account {account.Comp.Number} from the treasury of {ToPrettyString(station)}, paying by emission");
                Deposit(account.AsNullable(), salary, account.Comp.SalaryReason);
                return;
            }

            Deposit(account.AsNullable(), paid, account.Comp.SalaryReason);
            return;
        }

        Deposit(account.AsNullable(), salary, account.Comp.SalaryReason);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!_cfg.GetCVar(AquilaCVars.Economy))
            return;

        ProtoId<JobPrototype>? job = null;
        if (!string.IsNullOrEmpty(args.JobId))
            job = new ProtoId<JobPrototype>(args.JobId);

        if (job is null &&
            _stationJobs.TryGetPlayerJobs(args.Station, args.Player.UserId, out var playerJobs) &&
            playerJobs.Count > 0)
        {
            job = playerJobs[^1];
        }

        if (!_idCard.TryFindIdCard(args.Mob, out var idCard) || !TryComp<BankCardComponent>(idCard, out var card))
        {
            _pending[args.Mob] = new PendingAccount(job, args.Profile.Name, _timing.CurTime + TimeSpan.FromMinutes(10));
            return;
        }

        job ??= idCard.Comp.JobPrototype;
        SetupAccount(idCard, card, job, args.Profile.Name, args.Station.IsValid() ? args.Station : null);
    }

    private void UpdatePendingAccounts()
    {
        var now = _timing.CurTime;
        foreach (var (mob, pending) in _pending.ToArray())
        {
            if (TerminatingOrDeleted(mob) || now >= pending.Expiry)
            {
                _pending.Remove(mob);
                continue;
            }

            if (!_idCard.TryFindIdCard(mob, out var idCard) || !TryComp<BankCardComponent>(idCard, out var card))
                continue;

            var job = pending.Job ?? idCard.Comp.JobPrototype;
            SetupAccount(idCard, card, job, pending.Name, null);
            _pending.Remove(mob);
        }
    }

    private void SetupAccount(Entity<IdCardComponent> idCard, BankCardComponent card, ProtoId<JobPrototype>? job, string name, EntityUid? station)
    {
        station ??= _station.GetOwningStation(idCard.Owner);

        if (card.Account is { } existingUid && TryComp<BankAccountComponent>(existingUid, out var existing))
        {
            existing.Job = job;
            existing.Salary = ResolveSalary(existing, job);
            existing.Station ??= station;
            return;
        }

        var account = CreateAccount((idCard.Owner, card), name, job, station);
        account.Comp.Salary = ResolveSalary(account.Comp, job);

        var starting = account.Comp.Salary * account.Comp.StartingPaychecks;
        var paid = starting;
        if (station is { } stationUid
            && TryComp<StationTreasuryComponent>(stationUid, out var treasury)
            && treasury.PayStartingFromTreasury)
        {
            if (!TryPayFromTreasury(stationUid, starting, account.Comp.StartingReason, treasury.CoverStartingWithSubsidy, null, out paid, account.AsNullable())
                && paid == 0)
            {
                Log.Warning($"Failed to pay starting payment of {starting} credits to account {account.Comp.Number} from the treasury of {ToPrettyString(stationUid)}, paying by emission");
                paid = starting;
            }
        }

        Deposit(account.AsNullable(), paid, account.Comp.StartingReason);
    }

    private static int ResolveSalary(BankAccountComponent account, ProtoId<JobPrototype>? job)
    {
        return job is { } jobId
            ? account.Salaries.GetValueOrDefault(jobId, account.DefaultSalary)
            : account.DefaultSalary;
    }

    /// <summary>
    /// Проверяет, можно ли оформить на ID-карту новый банковский счёт: у карты его ещё нет.
    /// </summary>
    public bool CanOpenAccount(EntityUid idCard)
    {
        return _cfg.GetCVar(AquilaCVars.Economy)
               && HasComp<IdCardComponent>(idCard)
               && (!TryComp<BankCardComponent>(idCard, out var card) || card.Account == null);
    }

    /// <summary>
    /// Оформляет на ID-карту новый банковский счёт на имя, указанное на карте, а если его нет, на запасное имя. Такой счёт открывается пустым и без зарплаты.
    /// </summary>
    public bool TryOpenAccount(EntityUid idCard, string fallbackName, [NotNullWhen(true)] out Entity<BankAccountComponent>? account)
    {
        account = null;
        if (!CanOpenAccount(idCard))
            return false;

        var card = EnsureComp<BankCardComponent>(idCard);
        var id = Comp<IdCardComponent>(idCard);
        var ownerName = string.IsNullOrWhiteSpace(id.FullName) ? fallbackName : id.FullName;
        var created = CreateAccount((idCard, card), ownerName, id.JobPrototype, _station.GetOwningStation(idCard));
        account = created;
        return true;
    }

    private Entity<BankAccountComponent> CreateAccount(Entity<BankCardComponent> card, string ownerName, ProtoId<JobPrototype>? job, EntityUid? station)
    {
        var uid = Spawn(card.Comp.AccountPrototype);
        var account = Comp<BankAccountComponent>(uid);

        account.Number = GenerateNumber(account);
        account.OwnerName = ownerName;
        account.Job = job;
        account.Station = station;
        account.NextPayday = _timing.CurTime + account.PaydayInterval;
        account.Pin = _random.Next(MinPin, MaxPinExclusive);
        card.Comp.Account = uid;

        NotifyPinGenerated((uid, account), card.Owner);
        return (uid, account);
    }

    /// <summary>
    /// Проверяет PIN счёта. Если PIN не задан, подходит любое значение.
    /// </summary>
    /// <param name="account">Счёт, PIN которого проверяют.</param>
    /// <param name="pin">Проверяемый PIN. Значения 0 и меньше верными не считаются.</param>
    /// <returns>true, если PIN верный или на счёте он ещё не задан.</returns>
    public bool CheckPin(Entity<BankAccountComponent?> account, int pin)
    {
        if (!Resolve(account, ref account.Comp, false))
            return false;

        if (account.Comp.Pin == BankAccountComponent.NoPin)
            return true;

        return pin > 0 && pin == account.Comp.Pin;
    }

    /// <summary>
    /// Меняет PIN счёта, если старый PIN верный, а новый состоит из четырёх цифр. После успешной смены счётчик
    /// неудачных попыток ввода PIN сбрасывается.
    /// </summary>
    /// <param name="account">Счёт, PIN которого меняют.</param>
    /// <param name="oldPin">Текущий PIN счёта.</param>
    /// <param name="newPin">Новый PIN от 1000 до 9999.</param>
    /// <returns>true, если PIN изменён.</returns>
    public bool TryChangePin(Entity<BankAccountComponent?> account, int oldPin, int newPin)
    {
        if (!Resolve(account, ref account.Comp, false)
            || !CheckPin(account, oldPin)
            || newPin < MinPin
            || newPin >= MaxPinExclusive)
            return false;

        account.Comp.Pin = newPin;
        account.Comp.FailedPinAttempts = 0;
        return true;
    }

    /// <summary>
    /// Устанавливает уровень защиты счёта, если PIN верный. Счёт без PIN не может получить уровень выше открытого:
    /// иначе проверка PIN перестала бы что-либо защищать.
    /// </summary>
    /// <param name="account">Счёт, уровень защиты которого меняют.</param>
    /// <param name="level">Уровень защиты из <see cref="BankSecurityLevels"/>.</param>
    /// <param name="pin">Текущий PIN счёта.</param>
    /// <returns>true, если уровень защиты изменён.</returns>
    public bool TrySetSecurityLevel(Entity<BankAccountComponent?> account, int level, int pin)
    {
        if (!Resolve(account, ref account.Comp, false)
            || level < BankSecurityLevels.Open
            || level > BankSecurityLevels.Full
            || (account.Comp.Pin == BankAccountComponent.NoPin && level > BankSecurityLevels.Open)
            || !CheckPin(account, pin))
            return false;

        account.Comp.SecurityLevel = level;
        return true;
    }

    /// <summary>
    /// Сбрасывает PIN счёта и его уровень защиты до открытого, как будто PIN ещё не задан. Владелец после этого
    /// задаёт новый PIN в банкомате без старого.
    /// </summary>
    /// <param name="account">Счёт, чей PIN сбрасывают.</param>
    /// <returns>true, если PIN сброшен.</returns>
    public bool TryResetPin(Entity<BankAccountComponent?> account)
    {
        if (!Resolve(account, ref account.Comp, false))
            return false;

        account.Comp.Pin = BankAccountComponent.NoPin;
        account.Comp.SecurityLevel = BankSecurityLevels.Open;
        account.Comp.FailedPinAttempts = 0;
        account.Comp.LastFailedPinTime = default;
        return true;
    }

    private void NotifyPinGenerated(Entity<BankAccountComponent> account, EntityUid card)
    {
        if (FindCardHolder(card) is not { } holder)
            return;

        _popup.PopupEntity(Loc.GetString(account.Comp.PinGeneratedMessage, ("pin", account.Comp.Pin)), card, holder, PopupType.Medium);
    }

    private EntityUid? FindCardHolder(EntityUid card)
    {
        var current = Transform(card).ParentUid;
        while (current.IsValid())
        {
            if (HasComp<ActorComponent>(current))
                return current;

            current = Transform(current).ParentUid;
        }

        return null;
    }

    private void OnExamined(Entity<BankCardComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !TryGetAccount(ent.AsNullable(), out var account))
            return;

        args.PushMarkup(Loc.GetString("bank-card-examine-number", ("number", account.Value.Comp.Number.ToString())));
    }

    /// <summary>
    /// Ищет счёт, привязанный к карте.
    /// </summary>
    public bool TryGetAccount(Entity<BankCardComponent?> card, [NotNullWhen(true)] out Entity<BankAccountComponent>? account)
    {
        account = null;
        if (!Resolve(card, ref card.Comp, false)
            || card.Comp.Account is not { } uid
            || !TryComp<BankAccountComponent>(uid, out var comp))
            return false;

        account = (uid, comp);
        return true;
    }

    /// <summary>
    /// Отдаёт номер, баланс и зарплату счёта, привязанного к карте.
    /// </summary>
    public bool TryGetAccountInfo(Entity<BankCardComponent?> card, out int number, out int balance, out int salary)
    {
        if (!Resolve(card, ref card.Comp, false))
        {
            number = 0;
            balance = 0;
            salary = 0;
            return false;
        }

        if (!TryGetAccount(card, out var account))
        {
            number = 0;
            balance = 0;
            salary = 0;
            return false;
        }

        number = account.Value.Comp.Number;
        balance = account.Value.Comp.Balance;
        salary = account.Value.Comp.Salary;
        return true;
    }

    /// <summary>
    /// Ищет счёт по его номеру.
    /// </summary>
    public bool TryFindAccount(int number, [NotNullWhen(true)] out Entity<BankAccountComponent>? account)
    {
        var query = EntityQueryEnumerator<BankAccountComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Number != number)
                continue;

            account = (uid, comp);
            return true;
        }

        account = null;
        return false;
    }

    /// <summary>
    /// Зачисляет деньги на счёт.
    /// </summary>
    public void Deposit(Entity<BankAccountComponent?> account, int amount, LocId reason, string? party = null, string? comment = null)
    {
        if (!Resolve(account, ref account.Comp) || amount <= 0)
            return;

        account.Comp.Balance += amount;
        AddTransaction((account, account.Comp), amount, reason, party, comment);
    }

    /// <summary>
    /// Платит сумму из казны станции и сообщает, сколько вызывающему нужно зачислить на счёт получателя.
    /// Если в казне не хватает, она отдаёт всё, что есть, а недостачу при разрешённой субсидии сразу
    /// зачисляет на счёт получателя с причиной субсидии.
    /// </summary>
    /// <param name="station">Станция, из казны которой идёт выплата.</param>
    /// <param name="amount">Сумма выплаты.</param>
    /// <param name="reason">Описание списания из казны в журнале бюджета станции.</param>
    /// <param name="allowSubsidy">Покрывать ли нехватку казны субсидией ЦК.</param>
    /// <param name="party">Сторона операции для истории субсидии, по умолчанию ЦК.</param>
    /// <param name="paid">Сколько денег вызывающему нужно зачислить на счёт получателя.</param>
    /// <param name="account">Счёт получателя для зачисления субсидии. Без него субсидия не выдаётся.</param>
    /// <returns>true, если выплачено хоть что-то.</returns>
    public bool TryPayFromTreasury(
        EntityUid? station,
        int amount,
        LocId reason,
        bool allowSubsidy,
        string? party,
        out int paid,
        Entity<BankAccountComponent?> account = default)
    {
        paid = 0;
        if (amount <= 0
            || station is not { } stationUid
            || !TryComp<StationBankAccountComponent>(stationUid, out var bank)
            || !TryComp<StationTreasuryComponent>(stationUid, out var treasury))
            return false;

        var balance = Math.Max(0, _cargo.GetBalanceFromAccount((stationUid, bank), treasury.PayrollAccount));
        var fromTreasury = Math.Min(amount, balance);
        if (fromTreasury > 0 && !_cargo.TryAdjustBankAccount((stationUid, bank), treasury.PayrollAccount, -fromTreasury))
            return false;

        if (fromTreasury > 0)
            _budgetLog.LogTransaction(stationUid, treasury.PayrollAccount, -fromTreasury, reason, account.Comp?.OwnerName);

        paid = fromTreasury;
        if (fromTreasury >= amount)
            return true;

        if (!allowSubsidy || account.Comp is not { } payee)
            return paid > 0;

        Deposit((account.Owner, payee), amount - fromTreasury, treasury.SubsidyReason,
            party ?? Loc.GetString(payee.CentralCommandParty));
        return true;
    }

    /// <summary>
    /// Проверяет, пройдёт ли выплата из казны станции полностью, не трогая деньги.
    /// </summary>
    /// <param name="station">Станция, из казны которой идёт выплата.</param>
    /// <param name="amount">Сумма выплаты.</param>
    /// <param name="allowSubsidy">Считать ли нехватку покрытой субсидией ЦК.</param>
    /// <returns>true, если в казне хватает денег или нехватку покрывает субсидия.</returns>
    public bool CanPayFromTreasury(EntityUid? station, int amount, bool allowSubsidy)
    {
        if (amount <= 0
            || station is not { } stationUid
            || !TryComp<StationBankAccountComponent>(stationUid, out var bank)
            || !TryComp<StationTreasuryComponent>(stationUid, out var treasury))
            return false;

        var available = Math.Max(0, _cargo.GetBalanceFromAccount((stationUid, bank), treasury.PayrollAccount));
        return available >= amount || allowSubsidy;
    }

    /// <summary>
    /// Списывает деньги со счёта, если на нём хватает средств.
    /// </summary>
    /// <param name="account">Счёт, с которого списывают деньги.</param>
    /// <param name="amount">Сумма списания.</param>
    /// <param name="reason">Описание операции в истории.</param>
    /// <param name="party">Имя второй стороны операции, если она есть.</param>
    /// <param name="comment">Пояснение к операции, например назначение платежа.</param>
    public bool TryWithdraw(Entity<BankAccountComponent?> account, int amount, LocId reason, string? party = null, string? comment = null)
    {
        if (!Resolve(account, ref account.Comp) || amount <= 0 || account.Comp.Balance < amount)
            return false;

        account.Comp.Balance -= amount;
        AddTransaction((account, account.Comp), -amount, reason, party, comment);
        return true;
    }

    /// <summary>
    /// Проверяет, укладывается ли снятие наличных в предел счёта на одну операцию. Общий для всех способов снятия.
    /// </summary>
    /// <param name="account">Счёт, с которого снимают наличные.</param>
    /// <param name="amount">Сумма снятия.</param>
    /// <param name="limit">Предел снятия на счёте, 0 - без предела.</param>
    /// <returns>true, если снимать столько можно.</returns>
    public bool CanWithdrawCash(Entity<BankAccountComponent?> account, int amount, out int limit)
    {
        limit = 0;
        if (!Resolve(account, ref account.Comp))
            return false;

        limit = account.Comp.MaxCashWithdrawal;
        return limit <= 0 || amount <= limit;
    }

    /// <summary>
    /// Списывает деньги со счёта, даже если баланс уйдёт в минус. Долг гасится следующими зачислениями.
    /// </summary>
    public void Charge(Entity<BankAccountComponent?> account, int amount, LocId reason, string? party = null, string? comment = null)
    {
        if (!Resolve(account, ref account.Comp) || amount <= 0)
            return;

        account.Comp.Balance -= amount;
        AddTransaction((account, account.Comp), -amount, reason, party, comment);
    }

    /// <summary>
    /// Учитывает выписанную владельцем счёта премию, если она укладывается в лимит на смену.
    /// </summary>
    public bool TryReserveBonus(Entity<BankAccountComponent?> issuer, int amount, int limit)
    {
        if (!Resolve(issuer, ref issuer.Comp) || amount <= 0 || issuer.Comp.BonusesIssued + amount > limit)
            return false;

        issuer.Comp.BonusesIssued += amount;
        return true;
    }

    /// <summary>
    /// Переводит деньги с одного счёта на другой, если на счёте отправителя хватает средств.
    /// </summary>
    /// <param name="from">Счёт отправителя.</param>
    /// <param name="to">Счёт получателя.</param>
    /// <param name="amount">Сумма перевода.</param>
    /// <param name="outReason">Описание списания в истории отправителя, по умолчанию обычный перевод.</param>
    /// <param name="inReason">Описание зачисления в истории получателя, по умолчанию обычный перевод.</param>
    /// <param name="comment">Пояснение к операции, например назначение платежа.</param>
    public bool TryTransfer(Entity<BankAccountComponent?> from, Entity<BankAccountComponent?> to, int amount, LocId? outReason = null, LocId? inReason = null, string? comment = null)
    {
        if (!Resolve(from, ref from.Comp)
            || !Resolve(to, ref to.Comp)
            || from.Owner == to.Owner
            || !TryWithdraw(from, amount, outReason ?? from.Comp.TransferOutReason, to.Comp.OwnerName, comment))
            return false;

        Deposit(to, amount, inReason ?? to.Comp.TransferInReason, from.Comp.OwnerName, comment);
        return true;
    }

    private void AddTransaction(Entity<BankAccountComponent> account, int amount, LocId reason, string? party, string? comment = null)
    {
        var transaction = new BankTransaction
        {
            Time = _gameTicker.RoundDuration(),
            Amount = amount,
            Reason = reason,
            Party = party,
            Comment = comment,
        };

        account.Comp.History.Add(transaction);
        if (account.Comp.History.Count > account.Comp.MaxHistory)
            account.Comp.History.RemoveRange(0, account.Comp.History.Count - account.Comp.MaxHistory);

        var ev = new BankTransactionEvent(account, transaction);
        var query = EntityQueryEnumerator<BankCardComponent>();
        while (query.MoveNext(out var uid, out var card))
        {
            if (card.Account == account.Owner)
                RaiseLocalEvent(uid, ref ev);
        }

        RaiseLocalEvent(ref ev);
    }

    private int GenerateNumber(BankAccountComponent account)
    {
        int number;
        do
        {
            number = _random.Next(account.MinNumber, account.MaxNumber);
        }
        while (TryFindAccount(number, out _));

        return number;
    }
}
