namespace Content.Goobstation.Shared.Autosurgeon;

public sealed partial class AutoSurgeonComponent
{
    /// <summary>
    /// Создавать ли слот под орган, если его нет. Нужно для аугментов, у которых слот появляется только при операции.
    /// </summary>
    [DataField]
    public bool CreateOrganSlot;
}
