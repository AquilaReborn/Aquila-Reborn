using Content.Server.Guardian;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Guardian;
using Content.Shared.Aquila.HolyCorpse;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;

namespace Content.Server.Aquila.HolyCorpse;

public sealed class HolyCorpseCreatorSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly GuardianSystem _guardian = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolyCorpseCreatorComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<HolyCorpseCreatorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<HolyCorpseCreatorComponent, HolyCorpseCreatorDoAfterEvent>(OnDoAfter);
    }

    private void OnUseInHand(EntityUid uid, HolyCorpseCreatorComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryStartInjection(args.User, args.User, uid, component);
    }

    private void OnAfterInteract(EntityUid uid, HolyCorpseCreatorComponent component, AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        args.Handled = true;
        TryStartInjection(args.User, args.Target.Value, uid, component);
    }

    private void TryStartInjection(EntityUid user, EntityUid target, EntityUid item, HolyCorpseCreatorComponent component)
    {
        if (component.Used)
        {
            _popup.PopupEntity(Loc.GetString("guardian-activator-empty-invalid-creation"), user, user);
            return;
        }

        if (HasComp<GuardianHostComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("guardian-already-present-invalid-creation"), user, user);
            return;
        }

        // Вспышка крепится к цели сразу, существует только на время doAfter
        var flash = Spawn(component.FlashProto, Transform(target).Coordinates);
        _transform.SetParent(flash, target);

        var ev = new HolyCorpseCreatorDoAfterEvent(GetNetEntity(flash));
        var doAfterArgs = new DoAfterArgs(EntityManager, user, component.InjectionDelay, ev, item, target: target, used: item)
        {
            BreakOnMove = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            QueueDel(flash);
    }

    private void OnDoAfter(EntityUid uid, HolyCorpseCreatorComponent component, HolyCorpseCreatorDoAfterEvent args)
    {
        // Вспышка удаляется в любом случае: при успехе, отмене и обрыве
        if (args.FlashEntity != null)
            QueueDel(GetEntity(args.FlashEntity.Value));

        if (args.Handled || args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;

        if (args.Cancelled || component.Deleted || component.Used || HasComp<GuardianHostComponent>(target))
            return;

        if (_guardian.CreateGuardian(target, component.GuardianProto) == null)
            return;

        if (component.Components != null)
            EntityManager.AddComponents(target, component.Components);

        component.Used = true;
        args.Handled = true;
    }
}
