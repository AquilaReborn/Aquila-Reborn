using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Client.Audio;

public sealed partial class ClientGlobalSoundSystem
{
    private const float MinAdminMusicGain = 0.0001f;

    private float _adminVolumeOffset;

    private void OnAdminMusicVolumeChanged(float value)
    {
        var offset = SharedAudioSystem.GainToVolume(Math.Max(value, MinAdminMusicGain));
        var delta = offset - _adminVolumeOffset;
        _adminVolumeOffset = offset;

        foreach (var stream in _adminAudio)
        {
            if (TryComp<AudioComponent>(stream, out var audio))
                _audio.SetVolume(stream, audio.Params.Volume + delta, audio);
        }
    }
}
