using Content.Server.Atmos.Components;
using Content.Shared._Aquila.Audio;
using Content.Shared._Aquila.Chat;
using Content.Shared.Ghost;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Chat;

public sealed class VacuumChatSystem : EntitySystem
{
    private static readonly ProtoId<VacuumAudioPrototype> SettingsId = "Default";

    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InVacuumComponent, EmoteSoundAttemptEvent>(OnEmoteSoundAttempt);
    }

    /// <summary>
    /// Проверяет, может ли слушатель услышать речь источника.
    /// </summary>
    public bool CanHear(EntityUid source, EntityUid listener)
    {
        return GetHearing(source, listener) != VacuumHearing.None;
    }

    /// <summary>
    /// Определяет, как слушатель слышит речь источника с учётом вакуума.
    /// </summary>
    public VacuumHearing GetHearing(EntityUid source, EntityUid listener)
    {
        if (source == listener || HasComp<GhostComponent>(listener))
            return VacuumHearing.Air;

        if (!HasComp<InVacuumComponent>(source) && !HasComp<InVacuumComponent>(listener))
            return VacuumHearing.Air;

        if (_proto.TryIndex(SettingsId, out var settings)
            && HasSealedHelmet(source, settings)
            && HasSealedHelmet(listener, settings)
            && _transform.InRange(source, listener, settings.SuitCommsRange))
            return VacuumHearing.SuitComms;

        return VacuumHearing.None;
    }

    /// <summary>
    /// Оборачивает сообщение в чате пометкой локальной связи, если речь пришла по ней.
    /// </summary>
    public string WrapMessage(VacuumHearing hearing, string wrappedMessage)
    {
        return hearing == VacuumHearing.SuitComms
            ? Loc.GetString("vacuum-chat-suit-comms-wrap", ("message", wrappedMessage))
            : wrappedMessage;
    }

    private void OnEmoteSoundAttempt(Entity<InVacuumComponent> ent, ref EmoteSoundAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private bool HasSealedHelmet(EntityUid uid, VacuumAudioPrototype settings)
    {
        return _inventory.TryGetSlotEntity(uid, settings.HelmetSlot, out var head) && HasComp<PressureProtectionComponent>(head);
    }
}
