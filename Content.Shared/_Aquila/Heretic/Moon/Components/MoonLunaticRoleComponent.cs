using Content.Shared.Roles.Components;

namespace Content.Shared._Aquila.Heretic.Moon.Components;

/// <summary>
/// Роль разума лунатика: отделяет его от других ролей и позволяет снять её, когда безумие спадает.
/// </summary>
[RegisterComponent]
public sealed partial class MoonLunaticRoleComponent : BaseMindRoleComponent;
