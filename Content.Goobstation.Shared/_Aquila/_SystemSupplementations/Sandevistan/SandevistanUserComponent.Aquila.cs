namespace Content.Goobstation.Shared.Sandevistan;

public sealed partial class SandevistanUserComponent
{
    /// <summary>
    /// Сколько секунд живёт отпечаток следа, плавно растворяясь до полной прозрачности.
    /// </summary>
    [DataField]
    public float AfterimageLifetime = 0.6f;

    /// <summary>
    /// Оставляет ли владелец за собой след из отпечатков.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Afterimages = true;

    /// <summary>
    /// Видит ли владелец мир в замедленном, выцветшем виде, пока сандевистан включён.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TimeVision = true;
}
