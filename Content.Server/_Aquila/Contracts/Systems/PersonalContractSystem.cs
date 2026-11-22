using Content.Server._Aquila.Contracts.Components;
using Content.Server._Aquila.Contracts.DataDefinitions;
using Content.Server._Aquila.Economy.Components;
using Content.Server._Aquila.Economy.Systems;
using Content.Shared._Aquila.CCVars;
using Content.Shared.Access.Systems;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Body.Events;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Contracts.Systems;

/// <summary>
/// Личные контракты на работу: закрепляет работу за банковским счётом того, кто её взял, снимает закрепление
/// по истечении срока и выплачивает личную премию при сдаче. Награду того, кто выдал работу, не трогает:
/// премия начисляется дополнительно и из неё не вычитается.
/// </summary>
public sealed class PersonalContractSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly BankAccountSystem _bank = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly LocId PremiumReason = "bank-transaction-personal-contract";
    private static readonly LocId AlreadyTakenPopup = "personal-contract-already-taken";
    private static readonly LocId TakenPopup = "personal-contract-taken";
    private static readonly LocId PaidPopup = "personal-contract-paid";
    private static readonly LocId NoTreasuryPopup = "personal-contract-no-treasury";
    private static readonly LocId NoAccountPopup = "personal-contract-no-account";
    private static readonly LocId NoEmployerPopup = "personal-contract-no-employer";
    private static readonly LocId LimitPopup = "personal-contract-limit";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<CryostorageContainedComponent, ComponentStartup>(OnCryostorageStartup);
        SubscribeLocalEvent<ActorComponent, BeingGibbedEvent>(OnGibbed);
    }

    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        DropContracts(args.Entity);
    }

    private void OnCryostorageStartup(Entity<CryostorageContainedComponent> ent, ref ComponentStartup args)
    {
        DropContracts(ent.Owner);
    }

    private void OnGibbed(Entity<ActorComponent> ent, ref BeingGibbedEvent args)
    {
        foreach (var part in args.GibbedParts)
        {
            if (!TryGetUserAccount(part, out var accountNumber))
                continue;

            RemoveContracts(accountNumber);
            return;
        }

        DropContracts(ent.Owner);
    }

    private void DropContracts(EntityUid player)
    {
        if (!TryGetUserAccount(player, out var accountNumber))
            return;

        RemoveContracts(accountNumber);
    }

    private void RemoveContracts(int accountNumber)
    {
        var query = EntityQueryEnumerator<PersonalContractComponent>();
        while (query.MoveNext(out _, out var contracts))
        {
            var dropped = new List<ProtoId<CargoBountyPrototype>>();
            foreach (var (workId, contract) in contracts.Contracts)
            {
                if (contract.Account == accountNumber)
                    dropped.Add(workId);
            }

            if (dropped.Count == 0)
                continue;

            foreach (var workId in dropped)
                contracts.Contracts.Remove(workId);
        }
    }

    /// <summary>
    /// Проверяет, может ли участник экипажа взять работу на свой банковский счёт.
    /// </summary>
    /// <param name="employer">Тот, кто выдаёт работу и хранит контракты.</param>
    /// <param name="user">Участник экипажа, который берёт работу.</param>
    /// <param name="workId">Идентификатор работы.</param>
    /// <param name="accountNumber">Номер счёта, который получит премию.</param>
    /// <param name="quiet">Не показывать попапы об отказе.</param>
    /// <returns>True, если работу можно взять.</returns>
    public bool CanClaim(EntityUid employer, EntityUid user, ProtoId<CargoBountyPrototype> workId, out int accountNumber, bool quiet = false)
    {
        accountNumber = 0;

        if (TerminatingOrDeleted(employer))
            return Deny(NoEmployerPopup, user, quiet);

        if (!TryGetUserAccount(user, out accountNumber))
            return Deny(NoAccountPopup, user, quiet);

        if (!TryComp<PersonalContractComponent>(employer, out var contracts))
            return true;

        if (contracts.Contracts.TryGetValue(workId, out var contract) && !IsExpired(contracts, contract))
        {
            if (contract.Account != accountNumber)
                return Deny(AlreadyTakenPopup, user, quiet);

            return true;
        }

        if (CountActiveContracts(contracts, accountNumber) >= contracts.MaxActiveContracts)
            return Deny(LimitPopup, user, quiet, ("max", contracts.MaxActiveContracts));

        return true;
    }

    /// <summary>
    /// Закрепляет работу за банковским счётом участника экипажа и фиксирует личную премию.
    /// Повторное взятие той же работы тем же счётом оставляет прежние срок и премию без изменений.
    /// </summary>
    /// <param name="employer">Тот, кто выдаёт работу и хранит контракты.</param>
    /// <param name="user">Участник экипажа, который берёт работу.</param>
    /// <param name="workId">Идентификатор работы.</param>
    /// <param name="reward">Награда за работу, из которой считается премия.</param>
    /// <returns>
    /// True, если работу можно выдавать дальше. При выключенной экономике контракт не оформляется,
    /// но работа выдаётся как обычно: личных счетов всё равно нет.
    /// </returns>
    public bool TryClaim(EntityUid employer, EntityUid user, ProtoId<CargoBountyPrototype> workId, int reward)
    {
        if (!_cfg.GetCVar(AquilaCVars.Economy))
            return true;

        if (!CanClaim(employer, user, workId, out var accountNumber))
            return false;

        DoClaim(employer, user, workId, reward, accountNumber);
        return true;
    }

    /// <summary>
    /// Проверяет, взята ли работа именно этим участником экипажа и не истёк ли срок её закрепления.
    /// </summary>
    /// <param name="employer">Тот, кто выдаёт работу и хранит контракты.</param>
    /// <param name="user">Участник экипажа, который собирается печатать этикетку.</param>
    /// <param name="workId">Идентификатор работы.</param>
    /// <returns>True, если работу взял именно этот участник экипажа.</returns>
    public bool IsClaimedBy(EntityUid employer, EntityUid user, ProtoId<CargoBountyPrototype> workId)
    {
        if (!TryComp<PersonalContractComponent>(employer, out var contracts)
            || !contracts.Contracts.TryGetValue(workId, out var contract))
            return false;

        if (IsExpired(contracts, contract))
            return false;

        return TryGetUserAccount(user, out var accountNumber) && contract.Account == accountNumber;
    }

    /// <summary>
    /// Заполняет набор работ, квоты на которые уже взяты хоть кем-то и ещё не истекли.
    /// При выключенной экономике набор не пополняется.
    /// </summary>
    /// <param name="employer">Тот, кто выдаёт работу и хранит контракты.</param>
    /// <param name="jobs">Работы, которые сейчас видны на доске.</param>
    /// <param name="taken">Набор идентификаторов работ, который пополняется.</param>
    public void WriteTakenJobs(EntityUid employer, IReadOnlyCollection<ProtoId<CargoBountyPrototype>> jobs, HashSet<string> taken)
    {
        WriteJobs(employer, jobs, taken, null);
    }

    /// <summary>
    /// Заполняет набор работ, квоты на которые взял именно этот участник экипажа.
    /// При выключенной экономике набор не пополняется.
    /// </summary>
    /// <param name="employer">Тот, кто выдаёт работу и хранит контракты.</param>
    /// <param name="user">Участник экипажа, чьи квоты ищутся. При null набор не пополняется.</param>
    /// <param name="jobs">Работы, которые сейчас видны на доске.</param>
    /// <param name="mine">Набор идентификаторов работ, который пополняется.</param>
    public void WriteMineJobs(EntityUid employer, EntityUid? user, IReadOnlyCollection<ProtoId<CargoBountyPrototype>> jobs, HashSet<string> mine)
    {
        if (user is not { } viewer || !TryGetUserAccount(viewer, out var accountNumber))
            return;

        WriteJobs(employer, jobs, mine, accountNumber);
    }

    private void WriteJobs(
        EntityUid employer,
        IReadOnlyCollection<ProtoId<CargoBountyPrototype>> jobs,
        HashSet<string> result,
        int? accountNumber)
    {
        if (!_cfg.GetCVar(AquilaCVars.Economy))
            return;

        if (!TryComp<PersonalContractComponent>(employer, out var contracts))
            return;

        foreach (var job in jobs)
        {
            if (!contracts.Contracts.TryGetValue(job, out var contract)
                || IsExpired(contracts, contract)
                || (accountNumber is { } number && contract.Account != number))
                continue;

            result.Add(job.Id);
        }
    }

    private void DoClaim(EntityUid employer, EntityUid user, ProtoId<CargoBountyPrototype> workId, int reward, int accountNumber)
    {
        var contracts = EnsureComp<PersonalContractComponent>(employer);
        var contract = contracts.Contracts.GetValueOrDefault(workId);

        if (contract == null || IsExpired(contracts, contract))
        {
            contract = new PersonalContractData
            {
                Account = accountNumber,
                ClaimedAt = _timing.CurTime,
                Premium = GetPremium(contracts, reward),
            };
            contracts.Contracts[workId] = contract;
        }

        _popup.PopupEntity(Loc.GetString(TakenPopup, ("amount", contract.Premium)), user, user);
    }

    /// <summary>
    /// Выплачивает личную премию за сданную работу на счёт, который её взял, из казны станции.
    /// Если казна на станции не настроена, премия начисляется напрямую. Просроченный контракт
    /// не оплачивается и остаётся в списке до нового взятия работы. Контракт удаляется только после
    /// полной выплаты: если казна не может заплатить, он остаётся и сдать работу можно позже.
    /// Награду того, кто выдал работу, это не затрагивает.
    /// </summary>
    /// <param name="employer">Тот, кто выдаёт работу и хранит контракты.</param>
    /// <param name="workId">Идентификатор сданной работы.</param>
    /// <returns>True, если премия выплачена.</returns>
    public bool TryPay(EntityUid employer, ProtoId<CargoBountyPrototype> workId)
    {
        if (!_cfg.GetCVar(AquilaCVars.Economy))
            return false;

        if (!TryComp<PersonalContractComponent>(employer, out var contracts)
            || !contracts.Contracts.TryGetValue(workId, out var contract))
            return false;

        if (!CanPay(contracts, contract, out var account))
            return false;

        if (!DoPay(employer, account, contract))
            return false;

        contracts.Contracts.Remove(workId);
        return true;
    }

    private bool DoPay(EntityUid employer, Entity<BankAccountComponent> account, PersonalContractData contract)
    {
        if (!TryComp<StationTreasuryComponent>(employer, out var treasury)
            || !HasComp<StationBankAccountComponent>(employer))
        {
            _bank.Deposit(account.AsNullable(), contract.Premium, PremiumReason);
            PopupAccountHolder(contract.Account, Loc.GetString(PaidPopup, ("amount", contract.Premium)));
            return true;
        }

        if (!_bank.CanPayFromTreasury(employer, contract.Premium, treasury.CoverQuotaWithSubsidy)
            || !_bank.TryPayFromTreasury(employer, contract.Premium, PremiumReason, treasury.CoverQuotaWithSubsidy, null, out var paid, account.AsNullable()))
        {
            PopupAccountHolder(contract.Account, Loc.GetString(NoTreasuryPopup));
            return false;
        }

        _bank.Deposit(account.AsNullable(), paid, PremiumReason);
        PopupAccountHolder(contract.Account, Loc.GetString(PaidPopup, ("amount", contract.Premium)));
        return true;
    }

    private bool CanPay(
        PersonalContractComponent contracts,
        PersonalContractData contract,
        out Entity<BankAccountComponent> account)
    {
        account = default;

        if (IsExpired(contracts, contract) || contract.Premium <= 0)
            return false;

        if (!_bank.TryFindAccount(contract.Account, out var found))
            return false;

        account = found.Value;
        return true;
    }

    private bool IsExpired(PersonalContractComponent contracts, PersonalContractData contract)
    {
        return _timing.CurTime - contract.ClaimedAt >= contracts.ClaimDuration;
    }

    private int CountActiveContracts(PersonalContractComponent contracts, int accountNumber)
    {
        var count = 0;
        foreach (var contract in contracts.Contracts.Values)
        {
            if (contract.Account == accountNumber && !IsExpired(contracts, contract))
                count++;
        }

        return count;
    }

    private static int GetPremium(PersonalContractComponent contracts, int reward)
    {
        return Math.Min((int) (reward * contracts.PremiumShare), contracts.MaxPremium);
    }

    /// <summary>
    /// Ищет банковский счёт участника экипажа по его ID-карте.
    /// </summary>
    /// <param name="user">Участник экипажа, чей счёт ищется.</param>
    /// <param name="accountNumber">Номер найденного счёта.</param>
    /// <returns>True, если счёт найден.</returns>
    private bool TryGetUserAccount(EntityUid user, out int accountNumber)
    {
        accountNumber = 0;

        if (!_idCard.TryFindIdCard(user, out var idCard)
            || !_bank.TryGetAccount(idCard.Owner, out var account))
            return false;

        accountNumber = account.Value.Comp.Number;
        return true;
    }

    private bool Deny(LocId message, EntityUid user, bool quiet, params (string, object)[] args)
    {
        if (!quiet)
            _popup.PopupEntity(Loc.GetString(message, args), user, user);

        return false;
    }

    private void PopupAccountHolder(int number, string message)
    {
        var query = EntityQueryEnumerator<ActorComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (!_idCard.TryFindIdCard(uid, out var idCard)
                || !_bank.TryGetAccount(idCard.Owner, out var account)
                || account.Value.Comp.Number != number)
                continue;

            _popup.PopupEntity(message, uid, uid);
            return;
        }
    }
}
