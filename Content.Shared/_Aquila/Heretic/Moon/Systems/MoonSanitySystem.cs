using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Heretic.Moon.Systems;

/// <summary>
/// Общий помощник пути Луны для работы с уроном мозгу. Используется всеми способностями пути,
/// чтобы не дублировать поиск мозга и модификатор целостности органа.
/// </summary>
public sealed class MoonSanitySystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly TraumaSystem _trauma = default!;

    /// <summary>
    /// Статус-эффект лунного безумия, который накладывают все способности пути Луны.
    /// </summary>
    public static readonly EntProtoId InsanityStatus = "StatusEffectMoonInsanity";

    private const string BrainDamageKey = "MoonBrainDamage";

    /// <summary>
    /// Пытается найти мозг сущности.
    /// </summary>
    /// <param name="uid">Сущность, чей мозг ищем.</param>
    /// <param name="brain">Найденный мозг с компонентом органа.</param>
    /// <returns>True, если мозг найден.</returns>
    public bool TryGetBrain(EntityUid uid, out Entity<BrainComponent, OrganComponent> brain)
    {
        brain = default;

        if (!TryComp<BodyComponent>(uid, out var body))
            return false;

        if (!_body.TryGetBodyOrganEntityComps<BrainComponent>((uid, body), out var brains) || brains.Count == 0)
            return false;

        brain = brains[0];
        return true;
    }

    /// <summary>
    /// Возвращает накопленный урон мозгу от способностей пути Луны.
    /// </summary>
    /// <param name="uid">Сущность, чей урон проверяем.</param>
    /// <returns>Суммарный урон мозгу или ноль, если урона нет.</returns>
    public FixedPoint2 GetBrainDamage(EntityUid uid)
    {
        if (!TryGetBrain(uid, out var brain))
            return FixedPoint2.Zero;

        return brain.Comp2.IntegrityModifiers.TryGetValue((BrainDamageKey, uid), out var damage)
            ? damage
            : FixedPoint2.Zero;
    }

    /// <summary>
    /// Наносит урон мозгу.
    /// </summary>
    /// <param name="uid">Сущность, которой наносим урон.</param>
    /// <param name="amount">Величина урона.</param>
    public void ApplyBrainDamage(EntityUid uid, FixedPoint2 amount)
    {
        if (amount <= FixedPoint2.Zero || !TryGetBrain(uid, out var brain))
            return;

        if (!brain.Comp2.IntegrityModifiers.ContainsKey((BrainDamageKey, uid)))
            _trauma.TryCreateOrganDamageModifier(brain.Owner, amount, uid, BrainDamageKey, brain.Comp2);
        else
            _trauma.TryChangeOrganDamageModifier(brain.Owner, amount, uid, BrainDamageKey, brain.Comp2);
    }

    /// <summary>
    /// Восстанавливает урон мозгу, нанесённый способностями пути Луны.
    /// </summary>
    /// <param name="uid">Сущность, которой восстанавливаем мозг.</param>
    /// <param name="amount">Величина восстановления.</param>
    public void HealBrainDamage(EntityUid uid, FixedPoint2 amount)
    {
        if (amount <= FixedPoint2.Zero || !TryGetBrain(uid, out var brain))
            return;

        if (!brain.Comp2.IntegrityModifiers.TryGetValue((BrainDamageKey, uid), out var current))
            return;

        var healing = FixedPoint2.Min(amount, current);
        if (healing <= FixedPoint2.Zero)
            return;

        _trauma.TryChangeOrganDamageModifier(brain.Owner, -healing, uid, BrainDamageKey, brain.Comp2);
    }

    /// <summary>
    /// Снимает с мозга травмы органов, оставшиеся от физических повреждений.
    /// </summary>
    /// <param name="uid">Сущность, чей мозг очищаем.</param>
    public void RemoveBrainTraumas(EntityUid uid)
    {
        if (!TryComp<BodyComponent>(uid, out var body) ||
            !_trauma.TryGetBodyTraumas(uid, out var traumas, TraumaSystem.OrganDamage, body))
            return;

        foreach (var trauma in traumas)
        {
            if (trauma.Comp.TraumaTarget is { } target && HasComp<BrainComponent>(target))
                _trauma.RemoveTrauma(trauma);
        }
    }

    /// <summary>
    /// Проверяет, достаточно ли повреждён рассудок цели для безумия.
    /// </summary>
    /// <param name="uid">Проверяемая сущность.</param>
    /// <param name="threshold">Порог урона мозгу.</param>
    /// <returns>True, если урон мозгу не меньше порога.</returns>
    public bool IsInsane(EntityUid uid, float threshold)
    {
        return GetBrainDamage(uid) >= threshold;
    }
}
