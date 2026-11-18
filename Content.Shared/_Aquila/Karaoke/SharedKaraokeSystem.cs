using Robust.Shared.Audio.Systems;
using Content.Shared.Aquila.Karaoke.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Player;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Content.Shared.DeviceLinking;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling;
using Content.Shared.Humanoid;
using Content.Shared.Speech.Muting;
using Robust.Shared.Timing;

namespace Content.Shared.Aquila.Karaoke;

public abstract class SharedKaraokeSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AquilaKaraokePlayerComponent, ComponentInit>(OnKaraokePlayerInit);
        SubscribeLocalEvent<AquilaKaraokePlayerComponent, ComponentRemove>(OnKaraokePlayerRemove);
        SubscribeLocalEvent<AquilaKaraokePlayerComponent, EntInsertedIntoContainerMessage>(OnTapeInserted);
        SubscribeLocalEvent<AquilaKaraokePlayerComponent, EntRemovedFromContainerMessage>(OnTapeRemove);

        SubscribeLocalEvent<AquilaKaraokeMicComponent, PullStartedMessage>(OnMicPulled);
        SubscribeLocalEvent<AquilaKaraokeMicComponent, PullStoppedMessage>(OnMicGetOff);
    }

    private void OnKaraokePlayerInit(EntityUid uid, AquilaKaraokePlayerComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, "TapeContainer", component.TapeContainer);
    }

    private void OnKaraokePlayerRemove(EntityUid uid, AquilaKaraokePlayerComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.TapeContainer);
    }

    private void OnTapeInserted(EntityUid uid, AquilaKaraokePlayerComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (!TryComp(args.Entity, out AquilaKaraokeTapeComponent? tape) || tape.BackSound == null || !_timing.IsFirstTimePredicted)
            return;

        var mainAudio = _audio.PlayPvs(tape.BackSound, uid, AudioParams.Default.WithVolume(4f).WithMaxDistance(8f).WithPlayOffset(20f));

        if (mainAudio != null)
            comp.MainAudioStream = mainAudio.Value.Entity;
    }

    private void OnTapeRemove(EntityUid uid, AquilaKaraokePlayerComponent comp, EntRemovedFromContainerMessage args)
    {
        if (comp.MainAudioStream != null)
            _audio.Stop(comp.MainAudioStream);
    }

    private void OnMicPulled(EntityUid uid, AquilaKaraokeMicComponent comp, PullStartedMessage args)
    {

        if (args.PulledUid != uid || !_timing.IsFirstTimePredicted)
            return;

        var puller = args.PullerUid;

        if (!GetLinkedMachine(uid, out var karaoke))
            return;

        if (!TryComp<AquilaKaraokePlayerComponent>(karaoke, out var karaokeComp) || HasComp<MutedComponent>(puller) 
            || !TryComp<AudioComponent>(karaokeComp.MainAudioStream, out var audioComp) || !TryComp(karaokeComp.TapeContainer.Item, out AquilaKaraokeTapeComponent? tape)) // мы не хотим чтобы заглушеннный человек мог петь
            return;

        float offset = (float)(_timing.CurTime - audioComp.AudioStart).TotalSeconds;

        if (TryComp<HumanoidAppearanceComponent>(puller, out var appearance)) // Логично, что петь могут только гуманоиды.
        {
            if (appearance.Sex == Sex.Female)
            {
                _audio.Stop(karaokeComp.MainAudioStream);
                var femAudio = _audio.PlayPvs(tape.FemaleVoice, uid, AudioParams.Default.WithVolume(4f).WithMaxDistance(8f).WithPlayOffset(offset));
                if (femAudio != null)
                    karaokeComp.MainAudioStream = femAudio.Value.Entity;
            }
            else
            {
                _audio.Stop(karaokeComp.MainAudioStream);
                var maleAudio = _audio.PlayPvs(tape.MaleVoice, uid, AudioParams.Default.WithVolume(4f).WithMaxDistance(8f).WithPlayOffset(offset));
                if (maleAudio != null)
                    karaokeComp.MainAudioStream = maleAudio.Value.Entity;
            }
        }
    }

    private void OnMicGetOff(EntityUid uid, AquilaKaraokeMicComponent comp, PullStoppedMessage args)
    {
        if (_net.IsClient || args.PulledUid != uid)
            return;

        var puller = args.PullerUid;

        if (!GetLinkedMachine(uid, out var karaoke))
            return;

        if (!TryComp<AquilaKaraokePlayerComponent>(karaoke, out var karaokeComp))
            return;

        _audio.Stop(karaokeComp.MainAudioStream);
    }

    private bool GetLinkedMachine(EntityUid uid, out EntityUid? linkedEntity)
    {
        linkedEntity = null;

        if (TryComp<DeviceLinkSourceComponent>(uid, out var source))
        {
            foreach (var linked in source.LinkedPorts.Keys)
            {
                if (HasComp<AquilaKaraokePlayerComponent>(linked))
                {
                    linkedEntity = linked;
                    return true;
                }
            }
        }
        return false;
    }

}