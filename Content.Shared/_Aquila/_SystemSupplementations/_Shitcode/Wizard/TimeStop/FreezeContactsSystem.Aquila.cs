namespace Content.Shared._Goobstation.Wizard.TimeStop;

public sealed partial class FreezeContactsSystem
{
    private void MoveUpdateShutdown(Entity<FrozenComponent> ent, ref ComponentShutdown args)
    {
        _blocker.UpdateCanMove(ent.Owner);
    }
}
