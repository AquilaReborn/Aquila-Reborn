using Content.Shared._Aquila.Heretic.Moon.Components;
using Content.Shared._Aquila.Heretic.Moon.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Systems;
using Content.Shared.Gibbing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Popups;

namespace Content.Server._Aquila.Heretic.Moon.Systems;

/// <summary>
/// Пышное облачение пути Луны переводит весь входящий урон в урон мозгу носителя,
/// лишает его возможности нападать и разрывает ему голову при смерти.
/// </summary>
public sealed class MoonRegaliaSystem : SharedMoonRegaliaSystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MoonSanitySystem _sanity = default!;

    private static readonly LocId HeadExplodeMessage = "moon-regalia-head-explode";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoonRegaliaComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<MoonRegaliaComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<MoonRegaliaComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MoonRegaliaWearerComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
        SubscribeLocalEvent<MoonRegaliaWearerComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnEquipped(Entity<MoonRegaliaComponent> ent, ref GotEquippedEvent args)
    {
        ent.Comp.Wearer = args.Equipee;

        var wearer = EnsureComp<MoonRegaliaWearerComponent>(args.Equipee);
        wearer.BrainDamageMultiplier = ent.Comp.BrainDamageMultiplier;

        if (HasComp<PacifiedComponent>(args.Equipee))
            return;

        EnsureComp<PacifiedComponent>(args.Equipee);
        wearer.GrantedPacified = true;
    }

    private void OnUnequipped(Entity<MoonRegaliaComponent> ent, ref GotUnequippedEvent args)
    {
        ent.Comp.Wearer = null;
        RemoveWearer(args.Equipee);
    }

    private void OnShutdown(Entity<MoonRegaliaComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Wearer is not { } wearer)
            return;

        RemoveWearer(wearer);
    }

    private void RemoveWearer(EntityUid wearer)
    {
        if (TerminatingOrDeleted(wearer))
            return;

        if (!TryComp<MoonRegaliaWearerComponent>(wearer, out var regalia))
            return;

        if (regalia.GrantedPacified)
            RemComp<PacifiedComponent>(wearer);

        RemComp<MoonRegaliaWearerComponent>(wearer);
    }

    private void OnBeforeDamage(Entity<MoonRegaliaWearerComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled || args.Origin == null || args.Damage.GetTotal() <= 0)
            return;

        if (!_sanity.TryGetBrain(ent.Owner, out _))
            return;

        var total = args.Damage.GetTotal().Float() * ent.Comp.BrainDamageMultiplier;
        args.Cancelled = true;

        _sanity.ApplyBrainDamage(ent.Owner, total);
    }

    private void OnMobStateChanged(Entity<MoonRegaliaWearerComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || !HasComp<BodyComponent>(ent.Owner))
            return;

        foreach (var (head, _) in _body.GetBodyChildrenOfType(ent.Owner, BodyPartType.Head))
        {
            if (!_body.TryDetachPart(head))
                continue;

            var sound = TryComp<GibbableComponent>(head, out var gibbable) ? gibbable.GibSound : null;
            _body.GibPart(head, gibSoundOverride: sound);
            _popup.PopupEntity(Loc.GetString(HeadExplodeMessage, ("ent", ent.Owner)),
                ent.Owner,
                PopupType.MediumCaution);
            break;
        }
    }
}
