using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.Events;

/// <summary>
/// Запрос клиента показать PIN его счёта. Клиент отправляет его, когда вошёл в игру и уже знает свою сущность,
/// чтобы попап с PIN не потерялся во время загрузки.
/// </summary>
[Serializable, NetSerializable]
public sealed class BankPinRequestEvent : EntityEventArgs;
