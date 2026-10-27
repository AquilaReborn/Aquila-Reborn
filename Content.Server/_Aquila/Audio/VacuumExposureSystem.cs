using Content.Server.Atmos.EntitySystems;
using Content.Shared._Aquila.Audio;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Audio;

public sealed class VacuumExposureSystem : EntitySystem
{
    private static readonly ProtoId<VacuumAudioPrototype> SettingsId = "Default";

    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private float _updateTimer;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_proto.TryIndex(SettingsId, out var settings))
            return;

        _updateTimer -= frameTime;
        if (_updateTimer > 0f)
            return;

        _updateTimer = settings.UpdateInterval;

        var query = EntityQueryEnumerator<MobStateComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (IsInVacuum(uid, settings))
                EnsureComp<InVacuumComponent>(uid);
            else
                RemComp<InVacuumComponent>(uid);
        }
    }

    /// <summary>
    /// Проверяет, находится ли сущность в вакууме.
    /// </summary>
    public bool IsInVacuum(EntityUid uid)
    {
        return _proto.TryIndex(SettingsId, out var settings) && IsInVacuum(uid, settings);
    }

    private bool IsInVacuum(EntityUid uid, VacuumAudioPrototype settings)
    {
        var mixture = _atmosphere.GetContainingMixture(uid);
        return mixture == null || mixture.Pressure < settings.VacuumPressureThreshold;
    }
}
