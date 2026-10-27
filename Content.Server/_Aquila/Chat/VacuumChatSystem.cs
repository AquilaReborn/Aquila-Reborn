using Content.Server.Atmos.EntitySystems;
using Content.Shared._Aquila.Audio;
using Content.Shared._Aquila.Chat;
using Content.Shared.Ghost;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Chat;

public sealed class VacuumChatSystem : EntitySystem
{
    private static readonly ProtoId<VacuumAudioPrototype> SettingsId = "Default";

    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmoteSoundAttemptEvent>(OnEmoteSoundAttempt);
    }

    /// <summary>
    /// Проверяет, может ли слушатель услышать речь источника через воздух.
    /// </summary>
    public bool CanHear(EntityUid source, EntityUid listener)
    {
        if (source == listener || HasComp<GhostComponent>(listener) || !_proto.TryIndex(SettingsId, out var settings))
            return true;

        return !IsInVacuum(source, settings) && !IsInVacuum(listener, settings);
    }

    private void OnEmoteSoundAttempt(ref EmoteSoundAttemptEvent args)
    {
        if (args.Cancelled || !_proto.TryIndex(SettingsId, out var settings))
            return;

        args.Cancelled = IsInVacuum(args.Source, settings);
    }

    private bool IsInVacuum(EntityUid uid, VacuumAudioPrototype settings)
    {
        var mixture = _atmosphere.GetContainingMixture(uid);
        return mixture == null || mixture.Pressure < settings.SpeechPressureThreshold;
    }
}
