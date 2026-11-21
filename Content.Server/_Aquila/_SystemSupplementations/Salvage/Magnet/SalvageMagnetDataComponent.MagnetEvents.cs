namespace Content.Server.Salvage.Magnet;

public sealed partial class SalvageMagnetDataComponent
{
    /// <summary>
    /// Время удержания, заданное событием астероида для текущего предложения, если оно есть.
    /// </summary>
    [DataField, Access(typeof(SalvageSystem), Other = AccessPermissions.ReadExecute)]
    public TimeSpan? ActiveDuration;
}
