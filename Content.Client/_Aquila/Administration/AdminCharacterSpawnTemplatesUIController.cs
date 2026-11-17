using System.Linq;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Client._Aquila.Administration;

/// <summary>
/// Хранит шаблоны настроек админского спавна в локальном файле клиента.
/// </summary>
public sealed class AdminCharacterSpawnTemplatesUIController : UIController
{
    [Dependency] private readonly IResourceManager _resources = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    private static readonly ResPath SavePath = new("/admin_character_spawn_templates.yml");

    private AdminCharacterSpawnYamlStore<Dictionary<string, Dictionary<string, AdminCharacterSpawnTemplate>>> _store = default!;
    private Dictionary<string, Dictionary<string, AdminCharacterSpawnTemplate>> _templates = new();

    internal event Action<string>? TemplatesChanged;

    /// <summary>
    /// Загружает сохранённые шаблоны спавна из локального файла клиента.
    /// </summary>
    public override void Initialize()
    {
        _store = new(_resources.UserData, _serialization, Log, SavePath);
        _templates = _store.Load(_templates);
    }

    internal IReadOnlyDictionary<string, AdminCharacterSpawnTemplate> GetTemplates(string admin)
    {
        if (string.IsNullOrWhiteSpace(admin) || !_templates.TryGetValue(admin, out var templates) || templates == null)
            return new Dictionary<string, AdminCharacterSpawnTemplate>();

        return templates.Where(pair => pair.Value != null).ToDictionary(pair => pair.Key, pair => CopyTemplate(pair.Value));
    }

    internal bool HasTemplate(string admin, string name)
    {
        return !string.IsNullOrWhiteSpace(admin)
            && _templates.TryGetValue(admin, out var templates)
            && templates != null
            && templates.ContainsKey(name);
    }

    internal bool TrySaveTemplate(string admin, string name, AdminCharacterSpawnTemplate template)
    {
        if (string.IsNullOrWhiteSpace(admin) || string.IsNullOrWhiteSpace(name))
            return false;

        var templates = _templates.TryGetValue(admin, out var current) && current != null
            ? new Dictionary<string, AdminCharacterSpawnTemplate>(current)
            : new Dictionary<string, AdminCharacterSpawnTemplate>();
        name = name.Trim();
        if (templates.TryGetValue(name, out var existing) && existing != null)
            return false;

        templates[name] = CopyTemplate(template);
        return TrySaveAdminTemplates(admin, templates);
    }

    internal bool TryDeleteTemplate(string admin, string name)
    {
        if (string.IsNullOrWhiteSpace(admin) || string.IsNullOrWhiteSpace(name)
            || !_templates.TryGetValue(admin, out var current) || current == null)
            return false;

        var templates = new Dictionary<string, AdminCharacterSpawnTemplate>(current);
        return templates.Remove(name) && TrySaveAdminTemplates(admin, templates);
    }

    private bool TrySaveAdminTemplates(string admin, Dictionary<string, AdminCharacterSpawnTemplate> templates)
    {
        var updated = new Dictionary<string, Dictionary<string, AdminCharacterSpawnTemplate>>(_templates);
        if (templates.Count > 0)
            updated[admin] = templates;
        else
            updated.Remove(admin);

        if (!_store.TrySave(updated))
            return false;

        _templates = updated;
        TemplatesChanged?.Invoke(admin);
        return true;
    }

    private static AdminCharacterSpawnTemplate CopyTemplate(AdminCharacterSpawnTemplate template)
    {
        return new AdminCharacterSpawnTemplate
        {
            Gear = template.Gear,
            CustomGear = template.CustomGear?.Copy(),
            Implants = CopyList(template.Implants),
        };
    }

    private static List<T> CopyList<T>(List<T>? values)
    {
        return values == null ? new List<T>() : new List<T>(values);
    }
}
