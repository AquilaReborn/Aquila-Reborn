namespace Content.Shared.Administration;

public sealed partial class AdminFrozenSystem
{
    private void UpdateCanMoveShutdown(Entity<AdminFrozenComponent> ent, ref ComponentShutdown args)
    {
        _blocker.UpdateCanMove(ent.Owner);
    }
}
