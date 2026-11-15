using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;

namespace Content.Server.Ghost;

public sealed partial class GhostSystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;

    /// <summary>
    /// Возвращает прототип наблюдателя для разума: админам свой, остальным обычный.
    /// </summary>
    public EntProtoId GetObserverPrototype(MindComponent mind)
    {
        if (mind.UserId is { } userId
            && _player.TryGetSessionById(userId, out var session)
            && _adminManager.HasAdminFlag(session, AdminFlags.Admin))
            return GameTicker.AdminObserverPrototypeName;

        return GameTicker.ObserverPrototypeName;
    }
}
