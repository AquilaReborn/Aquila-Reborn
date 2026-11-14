using System.IO;
using System.Linq;
using Content.Shared.Roles;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client._Aquila.Administration;

/// <summary>
/// Хранит избранные наборы снаряжения администратора в локальном файле клиента.
/// </summary>
public sealed class AdminCharacterSpawnFavoritesUIController : UIController
{
    [Dependency] private readonly IResourceManager _resources = default!;

    [Dependency] private readonly ISerializationManager _serialization = default!;

    private static readonly ResPath SavePath = new("/admin_character_spawn_favorites.yml");
    private Dictionary<string, HashSet<string>> _favorites = new();

    /// <summary>
    /// Загружает избранные наборы экипировки из локального файла клиента.
    /// </summary>
    public override void Initialize()
    {
        try
        {
            using var reader = _resources.UserData.OpenText(SavePath);
            var document = DataNodeParser.ParseYamlStream(reader).First();
            _favorites = _serialization.Read<Dictionary<string, HashSet<string>>>(document.Root, notNullableOverride: true);
        }
        catch (FileNotFoundException)
        {
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to load admin character spawn favorites: {e}");
        }
    }

    internal bool IsFavorite(string admin, ProtoId<StartingGearPrototype> gear)
    {
        return !string.IsNullOrWhiteSpace(admin)
            && !string.IsNullOrWhiteSpace(gear.Id)
            && _favorites.TryGetValue(admin, out var favorites)
            && favorites?.Contains(gear.Id) == true;
    }

    internal void ToggleFavorite(string admin, ProtoId<StartingGearPrototype> gear)
    {
        if (string.IsNullOrWhiteSpace(admin) || string.IsNullOrWhiteSpace(gear.Id))
            return;

        if (!_favorites.TryGetValue(admin, out var favorites) || favorites == null)
        {
            favorites = new HashSet<string>();
            _favorites[admin] = favorites;
        }

        if (!favorites.Add(gear.Id))
        {
            favorites.Remove(gear.Id);
            if (favorites.Count == 0)
                _favorites.Remove(admin);
        }

        try
        {
            var node = _serialization.WriteValueAs<MappingDataNode>(_favorites);
            using var writer = _resources.UserData.OpenWriteText(SavePath);
            new YamlStream { new YamlDocument(node.ToYaml()) }.Save(writer, false);
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to save admin character spawn favorites: {e}");
        }
    }
}
