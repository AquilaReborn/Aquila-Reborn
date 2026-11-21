using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Aquila.Contracts.DataDefinitions;

/// <summary>
/// Личный контракт на работу: счёт исполнителя, время взятия и зафиксированная премия.
/// </summary>
[DataDefinition]
public sealed partial class PersonalContractData
{
    /// <summary>
    /// Номер счёта, который взял работу.
    /// </summary>
    [DataField]
    public int Account;

    /// <summary>
    /// Время, когда работа была взята. Сериализуется как смещение от текущего времени сервера,
    /// чтобы срок не сбивался после сохранения и загрузки карты.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ClaimedAt;

    /// <summary>
    /// Личная премия в кредитах, зафиксированная при взятии работы.
    /// </summary>
    [DataField]
    public int Premium;
}
