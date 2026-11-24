using Content.Shared.Heretic;
using Content.Shared.Interaction;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticRitualSystem
{
    private static readonly EntProtoId CodexProto = "CodexCicatrix";
    private static readonly LocId RuneErased = "heretic-rune-erased";

    /// <summary>
    /// Стирает руну трансмутации с эффектом успешного ритуала, если по ней кликнули Кодексом Цикатрикс.
    /// </summary>
    private bool TryEraseRuneWithCodex(Entity<HereticRitualRuneComponent> rune, InteractUsingEvent args)
    {
        if (args.Handled)
            return false;

        if (Prototype(args.Used) is not { } proto || proto.ID != CodexProto.Id)
            return false;

        if (!_heretic.TryGetHereticComponent(args.User, out _, out _))
            return false;

        RitualSuccess(rune.Owner, args.User, showPopup: false);
        _popup.PopupEntity(Loc.GetString(RuneErased), rune.Owner, args.User);
        QueueDel(rune.Owner);
        args.Handled = true;
        return true;
    }
}
