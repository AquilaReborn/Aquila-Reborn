namespace Content.Shared._Aquila.Surgery;

/// <summary>
/// Операция доступна, только если глубокий скан сканера имплантов опознал имплант, с которым она работает.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryIdentifiedConditionComponent : Component
{
    /// <summary>
    /// Слот органа, который надо проверить. Орган в нём должен быть опознан, если у него есть IdentificationRequired.
    /// </summary>
    [DataField]
    public string? OrganSlot;

    /// <summary>
    /// Должна ли быть опознана сама часть тела. Нужно для имплантов, встроенных прямо в конечность, например клинков богомола.
    /// </summary>
    [DataField]
    public bool Part;
}
