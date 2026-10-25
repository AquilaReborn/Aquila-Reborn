using System.Linq;
using Content.Server.NPC.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Aquila.LifeCreator;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Content.Goobstation.Maths.FixedPoint;

namespace Content.Server.Aquila.LifeCreator;

public sealed class SacrificeArmSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    private static readonly ProtoId<DamageTypePrototype> CutDamageType = "Slash";
    private const float CutDamageAmount = 10f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SacrificeArmActionEvent>(OnAction);
        SubscribeLocalEvent<LifeCreatorComponent, SacrificeArmDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<LifeCreatorPetComponent, AttackAttemptEvent>(OnPetAttackAttempt);
    }

    private void OnAction(SacrificeArmActionEvent args)
    {
        if (args.Handled) return;

        var performer = args.Performer;

        if (!HasComp<LifeCreatorComponent>(performer))
            return;

        var arms = _body.GetBodyChildrenOfType(performer, BodyPartType.Hand).ToList();
        if (arms.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("sacrifice-arm-no-arms"), performer, performer);
            return;
        }

        args.Handled = true;

        _popup.PopupEntity(Loc.GetString("sacrifice-arm-start"), performer, performer);

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            performer,
            TimeSpan.FromSeconds(3),
            new SacrificeArmDoAfterEvent(),
            performer)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };

        _pendingMobProto[performer] = args.MobPrototype;

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private readonly Dictionary<EntityUid, EntProtoId> _pendingMobProto = new();

    private void OnDoAfter(EntityUid performer, LifeCreatorComponent component, SacrificeArmDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled) return;
        args.Handled = true;

        if (!_pendingMobProto.TryGetValue(performer, out var mobProto))
            return;

        _pendingMobProto.Remove(performer);

        var arms = _body.GetBodyChildrenOfType(performer, BodyPartType.Hand).ToList();
        if (arms.Count == 0)
            return;

        var (armUid, _) = _random.Pick(arms);
        var spawnCoords = Transform(performer).Coordinates;
        
        _body.TryDetachPart(armUid);
        _body.GibPart(armUid);

        var cutDamage = new DamageSpecifier(_prototype.Index(CutDamageType), FixedPoint2.New(CutDamageAmount));
        _damageable.TryChangeDamage(performer, cutDamage, origin: performer);

        var mob = Spawn(mobProto, spawnCoords);

        _faction.IgnoreEntity(mob, performer);

        var pet = EnsureComp<LifeCreatorPetComponent>(mob);
        pet.Owner = performer;
    }

    private void OnPetAttackAttempt(EntityUid uid, LifeCreatorPetComponent component, AttackAttemptEvent args)
    {
        if (args.Cancelled || args.Target != component.Owner)
            return;

        args.Cancel();
    }
}
