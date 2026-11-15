namespace Content.Shared.PDA.Ringer;

public sealed partial class RingerComponent
{
    /// <summary>
    /// Беззвучный режим: уведомления только вибрируют, а рингтон не играет.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Silent;
}
