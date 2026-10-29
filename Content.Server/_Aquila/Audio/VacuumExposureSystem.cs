using Content.Server.Atmos.EntitySystems;
using Content.Shared._Aquila.Audio;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Audio;

public sealed class VacuumExposureSystem : EntitySystem
{
    private static readonly ProtoId<VacuumAudioPrototype> SettingsId = "Default";

    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateComponent, MapInitEvent>(OnMobMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_proto.TryIndex(SettingsId, out var settings))
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<VacuumExposureComponent>();
        while (query.MoveNext(out var uid, out var exposure))
        {
            if (exposure.NextCheck > now)
                continue;

            exposure.NextCheck = now + TimeSpan.FromSeconds(settings.UpdateInterval);

            if (IsInVacuum(uid, settings))
                EnsureComp<InVacuumComponent>(uid);
            else
                RemComp<InVacuumComponent>(uid);
        }
    }

    private void OnMobMapInit(Entity<MobStateComponent> ent, ref MapInitEvent args)
    {
        EnsureComp<VacuumExposureComponent>(ent);
    }

    private bool IsInVacuum(EntityUid uid, VacuumAudioPrototype settings)
    {
        var mixture = _atmosphere.GetContainingMixture(uid);
        return mixture == null || mixture.Pressure < settings.VacuumPressureThreshold;
    }
}
