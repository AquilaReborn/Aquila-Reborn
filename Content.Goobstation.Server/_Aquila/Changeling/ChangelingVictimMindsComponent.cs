namespace Content.Goobstation.Server._Aquila.Changeling;

/// <summary>
/// Запоминает разумы поглощённых генокрадом жертв по их ДНК, чтобы можно было передать им управление телом.
/// </summary>
[RegisterComponent]
public sealed partial class ChangelingVictimMindsComponent : Component
{
    /// <summary>
    /// Разумы жертв по ДНК их тел.
    /// </summary>
    [DataField]
    public Dictionary<string, EntityUid> Minds = new();
}
