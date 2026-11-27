namespace Content.Shared.Heretic.Components;

public sealed partial class HereticBladeComponent
{
    /// <summary>
    /// Шанс, с которым клинок Замка вырывает орган из выбранной части тела, один к пятнадцати.
    /// </summary>
    [DataField]
    [Access(Other = AccessPermissions.ReadExecute)]
    public float LockOrganChance = 1f / 15f;
}
