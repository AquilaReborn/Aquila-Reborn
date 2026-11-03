using Content.Goobstation.Shared.MantisBlades;
using Content.Server._Aquila.Cyberware.Components;
using Content.Shared._Aquila.Cyberware;
using Content.Shared._Aquila.Cyberware.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Cyberware;

/// <summary>
/// ЭМИ против установленных имплантов. Первый импульс только пробивает экранирование тела,
/// и лишь повторный импульс в течение короткого окна отключает импланты, причём ненадолго.
/// Чем больше железа в теле, тем сильнее бьёт отключение.
/// </summary>
public sealed class CyberwareEmpSystem : EntitySystem
{
    private static readonly LocId ShieldBrokenMessage = "cyberware-emp-shield-broken";
    private static readonly LocId DisabledMessage = "cyberware-emp-disabled";

    [Dependency] private readonly CyberwareBlockSystem _block = default!;
    [Dependency] private readonly CyberLoadSystem _cyberLoad = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedEmpSystem _emp = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberwareComponent, EmpAttemptEvent>(OnCyberwareEmpAttempt);
        SubscribeLocalEvent<CyberwareComponent, EmpPulseEvent>(OnCyberwareEmpPulse);
        SubscribeLocalEvent<MantisBladeArmComponent, EmpAttemptEvent>(OnMantisBladeEmpAttempt);
    }

    private void OnCyberwareEmpAttempt(Entity<CyberwareComponent> ent, ref EmpAttemptEvent args)
    {
        HandleEmpAttempt(ent, _block.GetHost(ent), ref args);
    }

    private void OnMantisBladeEmpAttempt(Entity<MantisBladeArmComponent> ent, ref EmpAttemptEvent args)
    {
        HandleEmpAttempt(ent, CompOrNull<BodyPartComponent>(ent)?.Body, ref args);
    }

    private void OnCyberwareEmpPulse(Entity<CyberwareComponent> ent, ref EmpPulseEvent args)
    {
        if (_block.GetHost(ent) == null)
            return;

        args.Affected = true;
        args.Disabled = true;
    }

    private void HandleEmpAttempt(EntityUid cyberware, EntityUid? host, ref EmpAttemptEvent args)
    {
        if (args.Cancelled
            || host is not { } body
            || !_cyberLoad.TryGetLoad(body, out var load))
            return;

        args.Cancelled = true;

        var now = _timing.CurTime;
        var tick = _timing.CurTick;

        if (tick == load.EmpShieldBrokenTick)
            return;

        if (now >= load.EmpVulnerableUntil)
        {
            load.EmpShieldBrokenTick = tick;
            load.EmpVulnerableUntil = now + load.EmpVulnerableWindow;
            _popup.PopupEntity(Loc.GetString(ShieldBrokenMessage), body, body, PopupType.MediumCaution);
            return;
        }

        _emp.DoEmpEffects(cyberware, 0f, load.EmpDisableTime);

        if (tick == load.LastEmpDamageTick)
            return;

        load.LastEmpDamageTick = tick;
        _popup.PopupEntity(Loc.GetString(DisabledMessage), body, body, PopupType.LargeCaution);

        var damage = new DamageSpecifier(_proto.Index(load.EmpDamageType), load.EmpDamageBase + load.Load * load.EmpDamagePerLoad);
        _damageable.TryChangeDamage(body, damage);
        _cyberLoad.AdjustStrain((body, load), load.Load * load.EmpStrainPerLoad);
    }
}
