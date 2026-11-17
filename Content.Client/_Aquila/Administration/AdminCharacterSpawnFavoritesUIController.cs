using Content.Shared.Roles;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Client._Aquila.Administration;

/// <summary>
/// Хранит избранные наборы снаряжения администратора в локальном файле клиента.
/// </summary>
public sealed class AdminCharacterSpawnFavoritesUIController : UIController
{
    [Dependency] private readonly IResourceManager _resources = default!;

    [Dependency] private readonly ISerializationManager _serialization = default!;

    private static readonly ResPath SavePath = new("/admin_character_spawn_favorites.yml");
    private AdminCharacterSpawnYamlStore<Dictionary<string, HashSet<string>>> _store = default!;
    private Dictionary<string, HashSet<string>> _favorites = new();

    /// <summary>
    /// Загружает избранные наборы экипировки из локального файла клиента.
    /// </summary>
    public override void Initialize()
    {
        _store = new(_resources.UserData, _serialization, Log, SavePath);
        _favorites = _store.Load(_favorites);
    }

    internal bool IsFavorite(string admin, ProtoId<StartingGearPrototype> gear)
    {
        return !string.IsNullOrWhiteSpace(admin)
            && !string.IsNullOrWhiteSpace(gear.Id)
            && _favorites.TryGetValue(admin, out var favorites)
            && favorites?.Contains(gear.Id) == true;
    }

    internal bool TryToggleFavorite(string admin, ProtoId<StartingGearPrototype> gear)
    {
        if (string.IsNullOrWhiteSpace(admin) || string.IsNullOrWhiteSpace(gear.Id))
            return false;

        var updated = new Dictionary<string, HashSet<string>>(_favorites);
        var favorites = updated.TryGetValue(admin, out var current) && current != null
            ? new HashSet<string>(current)
            : new HashSet<string>();
        if (!favorites.Add(gear.Id))
            favorites.Remove(gear.Id);
        if (favorites.Count == 0)
            updated.Remove(admin);
        else
            updated[admin] = favorites;

        if (!_store.TrySave(updated))
            return false;

        _favorites = updated;
        return true;
    }
}
