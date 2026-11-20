using Content.Shared.Mobs.Components;

namespace Content.Shared.Examine;

public abstract partial class ExamineSystemShared
{
    /// <summary>
    /// Checks whether the given examiner is unable to examine mobs because the examiner is in critical condition or dead.
    /// </summary>
    /// <remarks>
    /// Entities without <see cref="MobStateComponent"/> are never blocked by this check.
    /// </remarks>
    /// <param name="examiner">Entity that performs the examination.</param>
    /// <param name="examined">Entity that would be examined, if any.</param>
    /// <returns><see langword="true"/> if the examination is blocked for the examiner.</returns>
    public bool IsExamineBlockedAquila(EntityUid examiner, EntityUid? examined)
    {
        if (examined is not { } target || !HasComp<MobStateComponent>(target))
            return false;

        return MobStateSystem.IsCritical(examiner) || MobStateSystem.IsDead(examiner);
    }
}
