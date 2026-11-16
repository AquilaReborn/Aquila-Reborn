using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._Aquila.Economy.Components;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared._Aquila.CCVars;
using Content.Shared._Aquila.Economy.DataDefinitions;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
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
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;

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
                Deposit((uid, account), account.Salary, account.SalaryReason);
        }
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
        SetupAccount(idCard, card, job, args.Profile.Name);
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
            SetupAccount(idCard, card, job, pending.Name);
            _pending.Remove(mob);
        }
    }

    private void SetupAccount(Entity<IdCardComponent> idCard, BankCardComponent card, ProtoId<JobPrototype>? job, string name)
    {
        if (card.Account is { } existingUid && TryComp<BankAccountComponent>(existingUid, out var existing))
        {
            existing.Job = job;
            existing.Salary = ResolveSalary(existing, job);
            return;
        }

        var account = CreateAccount((idCard.Owner, card), name, job);
        account.Comp.Salary = ResolveSalary(account.Comp, job);

        Deposit(account.AsNullable(), account.Comp.Salary * account.Comp.StartingPaychecks, account.Comp.StartingReason);
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
        var created = CreateAccount((idCard, card), ownerName, id.JobPrototype);
        account = created;
        return true;
    }

    private Entity<BankAccountComponent> CreateAccount(Entity<BankCardComponent> card, string ownerName, ProtoId<JobPrototype>? job)
    {
        var uid = Spawn(card.Comp.AccountPrototype);
        var account = Comp<BankAccountComponent>(uid);

        account.Number = GenerateNumber(account);
        account.OwnerName = ownerName;
        account.Job = job;
        account.NextPayday = _timing.CurTime + account.PaydayInterval;
        card.Comp.Account = uid;
        return (uid, account);
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
    /// Списывает деньги со счёта, если на нём хватает средств.
    /// </summary>
    public bool TryWithdraw(Entity<BankAccountComponent?> account, int amount, LocId reason, string? party = null)
    {
        if (!Resolve(account, ref account.Comp) || amount <= 0 || account.Comp.Balance < amount)
            return false;

        account.Comp.Balance -= amount;
        AddTransaction((account, account.Comp), -amount, reason, party);
        return true;
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
    public bool TryTransfer(Entity<BankAccountComponent?> from, Entity<BankAccountComponent?> to, int amount)
    {
        if (!Resolve(from, ref from.Comp)
            || !Resolve(to, ref to.Comp)
            || from.Owner == to.Owner
            || !TryWithdraw(from, amount, from.Comp.TransferOutReason, to.Comp.OwnerName))
            return false;

        Deposit(to, amount, to.Comp.TransferInReason, from.Comp.OwnerName);
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
