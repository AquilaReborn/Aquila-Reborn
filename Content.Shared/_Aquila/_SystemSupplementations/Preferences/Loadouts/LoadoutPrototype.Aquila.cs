namespace Content.Shared.Preferences.Loadouts;

public sealed partial class LoadoutPrototype
{
    /// <summary>
    /// Удаляет старую вещь из слота, а не складывает её в рюкзак или руки. Вещь с содержимым не удаляется.
    /// </summary>
    [DataField]
    public bool Replace;
}
