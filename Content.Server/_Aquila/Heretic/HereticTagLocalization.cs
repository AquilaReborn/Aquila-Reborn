using Content.Shared.Tag;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Heretic;

/// <summary>
/// Переводит теги, которые требуются для ритуалов еретика, в подписи для игрока.
/// </summary>
public static class HereticTagLocalization
{
    /// <summary>
    /// Возвращает локализованное название тега, а если перевода нет - сам идентификатор.
    /// </summary>
    /// <param name="tag">Тег, который нужно подписать.</param>
    /// <param name="loc">Менеджер локализации.</param>
    /// <returns>Название тега для показа игроку.</returns>
    public static string Localize(ProtoId<TagPrototype> tag, ILocalizationManager loc)
    {
        return loc.TryGetString($"heretic-ritual-tag-{tag.Id.ToLowerInvariant()}", out var name) ? name : tag.Id;
    }
}
