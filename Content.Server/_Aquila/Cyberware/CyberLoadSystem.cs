using System.Diagnostics.CodeAnalysis;
using Content.Goobstation.Shared.MantisBlades;
using Content.Goobstation.Shared.RecoilAbsorber;
using Content.Goobstation.Shared.SmartLinkImplant;
using Content.Server._Aquila.Cyberware.Components;
using Content.Shared._Aquila.Cyberware;
using Content.Shared._Aquila.Cyberware.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Cyberware;

/// <summary>
/// Считает кибернагрузку тела и напряжение от свежих установок имплантов.
/// </summary>
public sealed class CyberLoadSystem : EntitySystem
{
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        SubscribeLocalEvent<CyberLoadComponent, BodyPartAddedEvent>(OnBodyChanged);
        SubscribeLocalEvent<CyberLoadComponent, BodyPartRemovedEvent>(OnBodyChanged);
        SubscribeLocalEvent<CyberLoadComponent, CyberwareStrainEvent>(OnCyberwareStrain);

        SubscribeLocalEvent<CyberwareComponent, GetCyberLoadEvent>(OnCyberwareGetLoad);
        SubscribeLocalEvent<CyberwareComponent, OrganAddedToBodyEvent>(OnOrganAddedToBody);
        SubscribeLocalEvent<CyberwareComponent, OrganRemovedFromBodyEvent>(OnOrganRemovedFromBody);
        SubscribeLocalEvent<CyberwareComponent, BodyPartAddedEvent>(OnCyberwarePartAdded);
        SubscribeLocalEvent<CyberwareComponent, ImplantImplantedEvent>(OnImplantImplanted);
        SubscribeLocalEvent<CyberwareComponent, ImplantRemovedEvent>(OnImplantRemoved);

        SubscribeLocalEvent<MantisBladeArmComponent, GetCyberLoadEvent>(OnMantisBladeGetLoad);
        SubscribeLocalEvent<SmartLinkArmComponent, GetCyberLoadEvent>(OnSmartLinkGetLoad);
        SubscribeLocalEvent<RecoilAbsorberArmComponent, GetCyberLoadEvent>(OnRecoilAbsorberGetLoad);

        SubscribeLocalEvent<MantisBladeArmComponent, ComponentStartup>(OnArmUpgradeChanged);
        SubscribeLocalEvent<MantisBladeArmComponent, ComponentShutdown>(OnArmUpgradeChanged);
        SubscribeLocalEvent<SmartLinkArmComponent, ComponentStartup>(OnArmUpgradeChanged);
        SubscribeLocalEvent<SmartLinkArmComponent, ComponentRemove>(OnArmUpgradeChanged);
        SubscribeLocalEvent<RecoilAbsorberArmComponent, ComponentStartup>(OnArmUpgradeChanged);
        SubscribeLocalEvent<RecoilAbsorberArmComponent, ComponentRemove>(OnArmUpgradeChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<CyberLoadComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NeedsRecalculation)
                Recalculate((uid, comp), !comp.IgnoreNextIncrease);

            if (now < comp.NextUpdate)
                continue;

            comp.NextUpdate = now + UpdateInterval;

            if (comp.Strain > 0f)
                comp.Strain = MathF.Max(0f, comp.Strain - comp.StrainDecay * (float) UpdateInterval.TotalSeconds);
        }
    }

    /// <summary>
    /// Возвращает актуальную кибернагрузку существа, при необходимости начиная её отслеживать.
    /// </summary>
    public bool TryGetLoad(EntityUid uid, [NotNullWhen(true)] out CyberLoadComponent? load)
    {
        load = null;
        if (!HasComp<BodyComponent>(uid))
            return false;

        if (!TryComp(uid, out load))
        {
            load = AddComp<CyberLoadComponent>(uid);
            Recalculate((uid, load), false);
            return true;
        }

        if (load.NeedsRecalculation)
            Recalculate((uid, load), !load.IgnoreNextIncrease);

        return true;
    }

    /// <summary>
    /// Риск киберпсихоза: постоянная нагрузка вместе с текущим напряжением за вычетом переносимости.
    /// </summary>
    public float GetRisk(Entity<CyberLoadComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return 0f;

        return MathF.Max(0f, ent.Comp.Load + ent.Comp.Strain - ent.Comp.Tolerance);
    }

    /// <summary>
    /// Меняет переносимость имплантов, не выходя за пределы от нуля до потолка тела.
    /// </summary>
    public void AdjustTolerance(Entity<CyberLoadComponent?> ent, float amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Tolerance = Math.Clamp(ent.Comp.Tolerance + amount, 0f, ent.Comp.MaxTolerance);
    }

    /// <summary>
    /// Меняет напряжение от свежих установок. Отрицательное значение снижает его.
    /// </summary>
    public void AdjustStrain(Entity<CyberLoadComponent?> ent, float amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Strain = MathF.Max(0f, ent.Comp.Strain + amount);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!HasComp<BodyComponent>(args.Mob))
            return;

        var comp = EnsureComp<CyberLoadComponent>(args.Mob);
        Recalculate((args.Mob, comp), false);
        comp.Strain = 0f;
    }

    private void OnCyberwareStrain(Entity<CyberLoadComponent> ent, ref CyberwareStrainEvent args)
    {
        AdjustStrain(ent.AsNullable(), args.Amount);
    }

    private void OnBodyChanged<T>(Entity<CyberLoadComponent> ent, ref T args)
    {
        ent.Comp.NeedsRecalculation = true;
    }

    private void OnCyberwareGetLoad(Entity<CyberwareComponent> ent, ref GetCyberLoadEvent args)
    {
        args.Load += ent.Comp.Load;
    }

    private void OnMantisBladeGetLoad(Entity<MantisBladeArmComponent> ent, ref GetCyberLoadEvent args)
    {
        args.Load += ent.Comp.CyberLoad;
    }

    private void OnSmartLinkGetLoad(Entity<SmartLinkArmComponent> ent, ref GetCyberLoadEvent args)
    {
        args.Load += ent.Comp.CyberLoad;
    }

    private void OnRecoilAbsorberGetLoad(Entity<RecoilAbsorberArmComponent> ent, ref GetCyberLoadEvent args)
    {
        args.Load += ent.Comp.CyberLoad;
    }

    private void OnOrganAddedToBody(Entity<CyberwareComponent> ent, ref OrganAddedToBodyEvent args)
    {
        QueueRecalculation(args.Body);
    }

    private void OnOrganRemovedFromBody(Entity<CyberwareComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        QueueRecalculation(args.OldBody);
    }

    private void OnCyberwarePartAdded(Entity<CyberwareComponent> ent, ref BodyPartAddedEvent args)
    {
        QueueRecalculation(args.Part.Comp.Body);
    }

    private void OnImplantImplanted(Entity<CyberwareComponent> ent, ref ImplantImplantedEvent args)
    {
        QueueRecalculation(args.Implanted);
    }

    private void OnImplantRemoved(Entity<CyberwareComponent> ent, ref ImplantRemovedEvent args)
    {
        QueueRecalculation(args.Implanted);
    }

    private void OnArmUpgradeChanged<TComp, TEvent>(Entity<TComp> ent, ref TEvent args) where TComp : IComponent
    {
        if (TryComp<BodyPartComponent>(ent.Owner, out var part))
            QueueRecalculation(part.Body);
    }

    /// <summary>
    /// Просит пересчитать нагрузку в ближайшем обновлении, не добавляя напряжения за её рост.
    /// Нужно, когда нагрузка меняется не из-за установки имплантов, например при снятии блокиратора.
    /// </summary>
    public void QueueRecalculationWithoutStrain(EntityUid body)
    {
        if (TerminatingOrDeleted(body) || !HasComp<BodyComponent>(body))
            return;

        var comp = EnsureComp<CyberLoadComponent>(body);
        comp.NeedsRecalculation = true;
        comp.IgnoreNextIncrease = true;
    }

    private void QueueRecalculation(EntityUid? body)
    {
        if (body is not { } uid
            || TerminatingOrDeleted(uid)
            || !HasComp<BodyComponent>(uid))
            return;

        EnsureComp<CyberLoadComponent>(uid).NeedsRecalculation = true;
    }

    private void Recalculate(Entity<CyberLoadComponent> ent, bool addStrain)
    {
        ent.Comp.NeedsRecalculation = false;

        var ev = new GetCyberLoadEvent();
        var count = 0;

        foreach (var (part, _) in _body.GetBodyChildren(ent.Owner))
        {
            CollectLoad(part, ref ev, ref count);
        }

        foreach (var (organ, _) in _body.GetBodyOrgans(ent.Owner))
        {
            CollectLoad(organ, ref ev, ref count);
        }

        if (_container.TryGetContainer(ent.Owner, ImplanterComponent.ImplantSlotId, out var implants))
        {
            foreach (var implant in implants.ContainedEntities)
            {
                CollectLoad(implant, ref ev, ref count);
            }
        }

        if (TryComp<CyberwareBlockedComponent>(ent, out var blocked))
            ev.Load *= blocked.LoadMultiplier;

        ent.Comp.IgnoreNextIncrease = false;

        if (addStrain && ev.Load > ent.Comp.Load)
            ent.Comp.Strain += (ev.Load - ent.Comp.Load) * ent.Comp.StrainMultiplier;

        ent.Comp.Load = ev.Load;
        ent.Comp.ImplantCount = count;
    }

    private void CollectLoad(EntityUid uid, ref GetCyberLoadEvent ev, ref int count)
    {
        var before = ev.Load;
        RaiseLocalEvent(uid, ref ev);

        if (ev.Load > before)
            count++;
    }
}
