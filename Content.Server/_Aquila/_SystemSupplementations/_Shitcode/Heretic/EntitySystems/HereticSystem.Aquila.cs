using Content.Shared.Eye;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticSystem
{
    /// <summary>
    /// Обновляет маску видимости еретика на теле после выдачи компонента.
    /// </summary>
    public void RefreshVisibility(Entity<EyeComponent?> body)
    {
        if (!Resolve(body, ref body.Comp, false) || body.Comp is not { } eye)
            return;

        _eye.SetVisibilityMask(body.Owner, eye.VisibilityMask | HereticVisFlags, eye);
    }
}
