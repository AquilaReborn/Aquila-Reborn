using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Cyberware;

/// <summary>
/// Временно навешивает компоненты из реестра и потом снимает только те, которых на сущности не было до этого.
/// </summary>
public sealed class TemporaryComponentsSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;

    /// <summary>
    /// Добавляет компоненты, которых на сущности ещё нет, и запоминает их имена в <paramref name="added"/>.
    /// </summary>
    public void AddMissing(EntityUid uid, ComponentRegistry components, List<string> added)
    {
        var toAdd = new ComponentRegistry();
        foreach (var (name, entry) in components)
        {
            if (HasComp(uid, entry.Component.GetType()))
                continue;

            toAdd.Add(name, entry);
            added.Add(name);
        }

        EntityManager.AddComponents(uid, toAdd);
    }

    /// <summary>
    /// Снимает компоненты, запомненные в <paramref name="added"/>, и очищает список.
    /// </summary>
    public void RemoveAdded(EntityUid uid, List<string> added)
    {
        foreach (var name in added)
        {
            if (_factory.TryGetRegistration(name, out var registration))
                RemComp(uid, registration.Type);
        }

        added.Clear();
    }
}
