namespace Content.Shared.Paper;

public sealed partial class PaperSystem
{
    private bool AquilaBlocksEditing(Entity<PaperComponent> entity, EntityUid user)
    {
        if (!entity.Comp.EditingDisabled)
            return false;

        _popupSystem.PopupClient(Loc.GetString("paper-tamper-proof-modified-message"), entity, user);
        return true;
    }
}
