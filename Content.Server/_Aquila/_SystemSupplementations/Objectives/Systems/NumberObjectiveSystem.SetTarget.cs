using Content.Server.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed partial class NumberObjectiveSystem
{
    /// <summary>
    /// Задаёт требуемое количество для числовой цели и обновляет её название и описание с новым счётчиком.
    /// Значение не может быть меньше единицы.
    /// </summary>
    public void SetTarget(EntityUid uid, int target, NumberObjectiveComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.Target = Math.Max(1, target);

        var meta = MetaData(uid);
        if (comp.Title != null)
            _metaData.SetEntityName(uid, Loc.GetString(comp.Title, ("count", comp.Target)), meta);

        if (comp.Description != null)
            _metaData.SetEntityDescription(uid, Loc.GetString(comp.Description, ("count", comp.Target)), meta);
    }
}
