namespace Content.Goobstation.Shared.Sandevistan;

public sealed partial class SandevistanUserComponent
{
    /// <summary>
    /// Сколько секунд живёт отпечаток следа, плавно растворяясь до полной прозрачности.
    /// </summary>
    [DataField]
    public float AfterimageLifetime = 0.6f;
}
