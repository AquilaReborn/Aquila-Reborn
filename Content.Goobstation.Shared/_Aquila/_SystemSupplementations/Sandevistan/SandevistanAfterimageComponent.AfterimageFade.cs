namespace Content.Goobstation.Shared.Sandevistan;

public sealed partial class SandevistanAfterimageComponent
{
    /// <summary>
    /// Когда отпечаток появился, от этого момента он начинает растворяться.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public TimeSpan SpawnTime;

    /// <summary>
    /// За сколько секунд отпечаток растворяется полностью.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Lifetime;
}
