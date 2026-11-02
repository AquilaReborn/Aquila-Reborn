using System.Linq;
using Content.Server._Aquila.Cyberware.Components;
using Content.Shared._Aquila.Cyberware;
using Content.Shared._Aquila.Cyberware.Components;
using Content.Shared.Body.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Cyberware;

/// <summary>
/// Ручной сканер имплантов: показывает импланты в теле, уровень кибернагрузки и признаки киберпсихоза.
/// </summary>
public sealed class CyberwareScannerSystem : EntitySystem
{
    private static readonly LocId CyberwareScannerDeepCooldown = "cyberware-scanner-deep-cooldown";
    private static readonly LocId CyberwareScannerBeingScanned = "cyberware-scanner-being-scanned";
    private static readonly LocId CyberwareScannerDeepStarted = "cyberware-scanner-deep-started";
    private static readonly LocId CyberwareScannerResult = "cyberware-scanner-result";
    private static readonly LocId CyberwareScannerAdapting = "cyberware-scanner-adapting";
    private static readonly LocId CyberwareScannerPsychosis = "cyberware-scanner-psychosis";
    private static readonly LocId CyberwareScannerCureProgress = "cyberware-scanner-cure-progress";
    private static readonly LocId CyberwareScannerImplants = "cyberware-scanner-implants";
    private static readonly LocId CyberwareScannerConcealed = "cyberware-scanner-concealed";
    private static readonly LocId CyberwareScannerDeepComplete = "cyberware-scanner-deep-complete";

    private static readonly LocId LevelNone = "cyberware-scanner-level-none";
    private static readonly LocId LevelLow = "cyberware-scanner-level-low";
    private static readonly LocId LevelElevated = "cyberware-scanner-level-elevated";
    private static readonly LocId LevelHigh = "cyberware-scanner-level-high";
    private static readonly LocId LevelCritical = "cyberware-scanner-level-critical";

    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly CyberLoadSystem _cyberLoad = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberwareScannerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CyberwareScannerComponent, CyberwareScanDoAfterEvent>(OnScanDoAfter);
    }

    /// <summary>
    /// Начинает сканирование цели. Цель видит, что её сканируют.
    /// Если цель лежит или связана, сканирование будет глубоким: дольше, и может опознать скрытые импланты.
    /// </summary>
    public bool TryStartScan(Entity<CyberwareScannerComponent?> scanner, EntityUid user, EntityUid target)
    {
        if (!Resolve(scanner, ref scanner.Comp)
            || !HasComp<BodyComponent>(target))
            return false;

        var deep = target != user && CanDeepScan(target);
        if (deep
            && TryComp<ImplantsIdentifiedComponent>(target, out var identified)
            && _timing.CurTime < identified.NextDeepScan)
        {
            deep = false;
            _popup.PopupEntity(Loc.GetString(CyberwareScannerDeepCooldown), target, user, PopupType.Medium);
        }

        var time = deep ? scanner.Comp.DeepScanTime : scanner.Comp.ScanTime;
        var doAfter = new DoAfterArgs(EntityManager, user, time, new CyberwareScanDoAfterEvent(deep), scanner.Owner, target: target, used: scanner.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return false;

        if (user != target)
        {
            _popup.PopupEntity(Loc.GetString(CyberwareScannerBeingScanned, ("user", Identity.Entity(user, EntityManager))),
                target,
                target,
                PopupType.MediumCaution);
        }

        if (deep)
            _popup.PopupEntity(Loc.GetString(CyberwareScannerDeepStarted), target, user, PopupType.Medium);

        return true;
    }

    private bool CanDeepScan(EntityUid target)
    {
        return _standing.IsDown(target)
            || TryComp<CuffableComponent>(target, out var cuffs) && cuffs.CuffedHandCount > 0;
    }

    private void OnAfterInteract(Entity<CyberwareScannerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled
            || !args.CanReach
            || args.Target is not { } target)
            return;

        args.Handled = TryStartScan(ent.AsNullable(), args.User, target);
    }

    private void OnScanDoAfter(Entity<CyberwareScannerComponent> ent, ref CyberwareScanDoAfterEvent args)
    {
        if (args.Cancelled
            || args.Handled
            || args.Target is not { } target)
            return;

        args.Handled = true;

        if (!_cyberLoad.TryGetLoad(target, out var load))
            return;

        var level = Loc.GetString(GetLevel((target, load)));
        var report = Loc.GetString(CyberwareScannerResult, ("count", load.ImplantCount), ("level", level));

        if (load.Strain > 0f)
            report += "\n" + Loc.GetString(CyberwareScannerAdapting);

        report += GetImplantReport(ent, target, args.Deep);

        if (TryComp<CyberpsychosisComponent>(target, out var psychosis)
            && !string.IsNullOrEmpty(psychosis.Stage.Id)
            && _proto.TryIndex(psychosis.Stage, out var stage))
        {
            report += "\n" + Loc.GetString(CyberwareScannerPsychosis, ("stage", Loc.GetString(stage.Name)));

            if (psychosis.CureProgress > 0f)
                report += "\n" + Loc.GetString(CyberwareScannerCureProgress, ("percent", (int) (psychosis.CureProgress / psychosis.CureTime * 100f)));
        }

        _popup.PopupEntity(report, target, args.User, PopupType.Medium);
    }

    private string GetImplantReport(Entity<CyberwareScannerComponent> scanner, EntityUid target, bool deep)
    {
        if (!_container.TryGetContainer(target, ImplanterComponent.ImplantSlotId, out var implants))
            return string.Empty;

        ImplantsIdentifiedComponent? identified = null;
        if (deep)
        {
            identified = EnsureComp<ImplantsIdentifiedComponent>(target);
            identified.NextDeepScan = _timing.CurTime + scanner.Comp.DeepScanCooldown;
        }
        else
        {
            TryComp(target, out identified);
        }

        var names = new List<string>();
        var concealed = 0;
        foreach (var implant in implants.ContainedEntities)
        {
            if (!HasComp<SubdermalImplantComponent>(implant))
                continue;

            var known = !IsConcealed(scanner.Comp, implant)
                || identified != null && identified.Identified.Contains(implant)
                || deep && _random.Prob(scanner.Comp.IdentifyChance);

            if (!known)
            {
                concealed++;
                continue;
            }

            if (deep && identified!.Identified.Add(implant))
                Dirty(target, identified);

            names.Add(Name(implant));
        }

        var report = string.Empty;
        if (names.Count > 0)
            report += "\n" + Loc.GetString(CyberwareScannerImplants, ("implants", string.Join(", ", names)));

        if (concealed > 0)
            report += "\n" + Loc.GetString(CyberwareScannerConcealed, ("count", concealed));

        if (deep)
            report += "\n" + Loc.GetString(CyberwareScannerDeepComplete);

        return report;
    }

    private bool IsConcealed(CyberwareScannerComponent scanner, EntityUid implant)
    {
        if (MetaData(implant).EntityPrototype is not { } proto)
            return false;

        foreach (var id in scanner.Concealed)
        {
            if (proto.ID == id.Id || proto.Parents?.Contains(id.Id) == true)
                return true;
        }

        return false;
    }

    private LocId GetLevel(Entity<CyberLoadComponent> ent)
    {
        var risk = _cyberLoad.GetRisk(ent.AsNullable());

        if (risk <= 0f)
            return LevelNone;

        if (risk < ent.Comp.WarningLevel)
            return LevelLow;

        if (risk < ent.Comp.Threshold)
            return LevelElevated;

        return risk < ent.Comp.CriticalLevel ? LevelHigh : LevelCritical;
    }
}
