namespace Content.Server._Aquila.Chat;

/// <summary>
/// Способ, которым слушатель слышит речь, когда кто-то из собеседников в вакууме.
/// </summary>
public enum VacuumHearing : byte
{
    None,
    Air,
    SuitComms,
}
