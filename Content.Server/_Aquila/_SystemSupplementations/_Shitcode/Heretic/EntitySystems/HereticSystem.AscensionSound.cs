using Robust.Shared.Audio;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticSystem
{
    private static readonly string MoonAscensionPath = "moon";
    private static readonly string LockAscensionPath = "lock";

    private static readonly string DefaultAscensionSoundPath =
        "/Audio/_Goobstation/Heretic/Ambience/Antag/Heretic/ascend_{0}.ogg";

    private static readonly SoundPathSpecifier MoonAscensionSound =
        new("/Audio/_Aquila/Heretic/Moon/ascend_moon.ogg");

    private static readonly SoundPathSpecifier LockAscensionSound =
        new("/Audio/_Aquila/Heretic/Lock/ascend_lock.ogg");

    /// <summary>
    /// Возвращает звук вознесения для указанного пути.
    /// </summary>
    /// <param name="pathLoc">Имя пути в нижнем регистре.</param>
    /// <returns>Звук вознесения.</returns>
    private SoundSpecifier GetAscensionSound(string pathLoc)
    {
        if (pathLoc == MoonAscensionPath)
            return MoonAscensionSound;

        if (pathLoc == LockAscensionPath)
            return LockAscensionSound;

        return new SoundPathSpecifier(string.Format(DefaultAscensionSoundPath, pathLoc));
    }
}
