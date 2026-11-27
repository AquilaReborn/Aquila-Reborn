using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server._Aquila.Heretic.Ritual.Helpers;

/// <summary>
/// Отбирает ограниченное число мёртвых жертв по дополнительному условию ритуала.
/// </summary>
public static class RitualSacrificeSelectionHelper
{
    /// <summary>
    /// Возвращает не более заданного числа существующих трупов, которые подходят ритуалу.
    /// </summary>
    public static List<EntityUid> SelectCorpses(IEntityManager entities, IEnumerable<EntityUid> candidates,
        float maximum, Func<EntityUid, bool> isEligible)
    {
        var selected = new List<EntityUid>();
        if (maximum <= 0)
            return selected;

        foreach (var uid in candidates)
        {
            if (!entities.TryGetComponent<MobStateComponent>(uid, out var mob) ||
                mob.CurrentState != MobState.Dead || entities.IsQueuedForDeletion(uid) || !isEligible(uid))
                continue;

            selected.Add(uid);
            if (selected.Count >= maximum)
                break;
        }

        return selected;
    }
}
