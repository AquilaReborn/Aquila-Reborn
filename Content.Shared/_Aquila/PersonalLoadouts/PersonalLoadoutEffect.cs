using System.Diagnostics.CodeAnalysis;
using Content.Shared.Clothing;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._Aquila.PersonalLoadouts;

/// <summary>
/// Делает лоадаут доступным только игрокам с указанными UUID.
/// </summary>
public sealed partial class PersonalLoadoutEffect : LoadoutEffect
{
    /// <summary>
    /// UUID игроков, которым принадлежит этот лоадаут.
    /// </summary>
    [DataField(required: true)]
    public List<string> Users = new();

    /// <summary>
    /// Роли, у которых лоадаут появляется во вкладке "Мой инвентарь". Пустой список - у всех ролей.
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> Roles = new();

    /// <summary>
    /// Разрешён ли лоадаут для указанной роли.
    /// </summary>
    public bool IsAllowedRole(ProtoId<RoleLoadoutPrototype> role)
    {
        if (Roles.Count == 0)
            return true;

        foreach (var job in Roles)
        {
            if (LoadoutSystem.GetJobPrototype(job) == role.Id)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Разрешён ли лоадаут для указанной роли с учётом всех его личных эффектов.
    /// </summary>
    public static bool IsAvailableForRole(LoadoutPrototype proto, ProtoId<RoleLoadoutPrototype> role)
    {
        foreach (var effect in proto.Effects)
        {
            if (effect is PersonalLoadoutEffect personal && !personal.IsAllowedRole(role))
                return false;
        }

        return true;
    }

    public bool IsOwner(NetUserId userId)
    {
        foreach (var user in Users)
        {
            if (Guid.TryParse(user, out var guid) && guid == userId.UserId)
                return true;
        }

        return false;
    }

    public override bool Validate(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        LoadoutPrototype proto,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        if (session == null || !IsOwner(session.UserId))
        {
            reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString("loadout-personal-only"));
            return false;
        }

        if (!IsAllowedRole(loadout.Role))
        {
            reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString("loadout-personal-wrong-role"));
            return false;
        }

        reason = null;
        return true;
    }
}
