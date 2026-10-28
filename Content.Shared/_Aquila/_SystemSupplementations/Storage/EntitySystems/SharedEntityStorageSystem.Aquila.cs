using Robust.Shared.Audio;

namespace Content.Shared.Storage.EntitySystems;

public abstract partial class SharedEntityStorageSystem
{
    private void PlayStorageSound(Robust.Shared.Audio.SoundSpecifier sound, EntityUid uid, EntityUid? user)
    {
        if (user != null)
            _audio.PlayPredicted(sound, uid, user);
        else if (_net.IsServer)
            _audio.PlayPvs(sound, uid);
    }
}
