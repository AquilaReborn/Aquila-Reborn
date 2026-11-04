using Content.Server._Aquila.Cyberware.Components;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Cyberware;

/// <summary>
/// Временно снижает шанс, что киберпсихоз начнётся или станет тяжелее.
/// </summary>
public sealed class CyberpsychosisResistanceSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberpsychosisResistanceComponent, GetCyberpsychosisChanceEvent>(OnGetCyberpsychosisChance);
    }

    /// <summary>
    /// Даёт защиту на время. Если защита уже есть, продлевает её и оставляет самый сильный множитель.
    /// </summary>
    public void AddResistance(EntityUid uid, TimeSpan time, float multiplier)
    {
        var now = _timing.CurTime;
        var comp = EnsureComp<CyberpsychosisResistanceComponent>(uid);

        comp.Multiplier = now < comp.EndTime ? MathF.Min(comp.Multiplier, multiplier) : multiplier;

        var end = now + time;
        if (end > comp.EndTime)
            comp.EndTime = end;
    }

    private void OnGetCyberpsychosisChance(Entity<CyberpsychosisResistanceComponent> ent, ref GetCyberpsychosisChanceEvent args)
    {
        if (_timing.CurTime >= ent.Comp.EndTime)
        {
            RemCompDeferred<CyberpsychosisResistanceComponent>(ent);
            return;
        }

        args.Multiplier *= ent.Comp.Multiplier;
    }
}
