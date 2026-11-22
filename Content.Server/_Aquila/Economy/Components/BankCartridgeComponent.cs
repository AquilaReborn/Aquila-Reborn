using Content.Server._Aquila.Economy.DataDefinitions;
using Content.Server._Aquila.Economy.Systems;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Economy.Components;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(BankCartridgeSystem))]
public sealed partial class BankCartridgeComponent : Component
{
    /// <summary>
    /// Кто и из какого бюджета может выписывать премии и штрафы. Действует первое совпадение по доступу.
    /// </summary>
    [DataField]
    public List<BankAuthority> Authorities = new();

    /// <summary>
    /// Наибольшая длина причины премии или штрафа.
    /// </summary>
    [DataField]
    public int MaxCommentLength = 64;

    /// <summary>
    /// Должности, которые подтверждают премии сверх лимита и штрафы больше потолка, по старшинству. Подтверждает только старшая из тех, что сейчас на смене, а её собственные премии и штрафы проходят сразу.
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> Approvers = new();

    /// <summary>
    /// Звук успешной операции в программе.
    /// </summary>
    [DataField]
    public SoundSpecifier? SuccessSound = new SoundPathSpecifier("/Audio/_Aquila/Economy/terminal_success.ogg", AudioParams.Default.WithVolume(-7f));

    /// <summary>
    /// Звук зачисления денег на счёт, который слышит только владелец КПК.
    /// </summary>
    [DataField]
    public SoundSpecifier? IncomeSound = new SoundPathSpecifier("/Audio/_Aquila/Economy/terminal_success.ogg", AudioParams.Default.WithVolume(-7f));

    /// <summary>
    /// Зачисления, при которых звук зачисления не играет, потому что у операции уже есть свой звук.
    /// </summary>
    [DataField]
    public List<LocId> SilentIncomeReasons = new() { "bank-transaction-atm-deposit", "bank-transaction-chip-deposit" };

    /// <summary>
    /// Когда программа в последний раз проигрывала звук. За один тик звучит только один звук.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan LastSoundTime;

    /// <summary>
    /// Звук отказа в операции.
    /// </summary>
    [DataField]
    public SoundSpecifier? DenySound = new SoundPathSpecifier("/Audio/_Aquila/Economy/terminal_prompt_deny.ogg", AudioParams.Default.WithVolume(-7f));

    /// <summary>
    /// Должность подтверждающего, найденная последним поиском. Null, если подходящих на смене нет.
    /// </summary>
    public ProtoId<JobPrototype>? CachedApproverJob;

    /// <summary>
    /// Когда должность подтверждающего искали последний раз. Null, если ещё не искали.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan? ApproverJobCacheTime;
}
