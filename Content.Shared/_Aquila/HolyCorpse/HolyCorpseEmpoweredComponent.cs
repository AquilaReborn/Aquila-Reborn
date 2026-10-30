using Robust.Shared.GameStates;

namespace Content.Shared.Aquila.HolyCorpse;

/// <summary>
/// Страж, усиленный дополнительными конечностями святого трупа.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(HolyCorpseEmpowerSystem))]
public sealed partial class HolyCorpseEmpoweredComponent : Component
{
    /// <summary>
    /// Множитель урона, скорости атаки, урона по выносливости и скорострельности стража.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Multiplier = 1f;
}
