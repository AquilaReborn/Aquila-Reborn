using System.Numerics;
using Content.Shared._Goobstation.Wizard.Guardian;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Guardian;

public sealed partial class GuardianSystem
{
    private const float MinRepositionDistanceSquared = 0.0001f;

    private void RepositionGuardian(EntityUid guardianUid, GuardianComponent guardian, TransformComponent guardianXform, EntityCoordinates coordinates)
    {
        guardian.IsBeginRepositioned = true;
        try
        {
            _transform.SetCoordinates(guardianUid, guardianXform, coordinates);
        }
        finally
        {
            guardian.IsBeginRepositioned = false;
        }
    }

    private void PullGuardianToHost(EntityUid guardianUid, GuardianComponent guardian, TransformComponent guardianXform, Vector2 hostPos, Vector2 diff)
    {
        if (diff.LengthSquared() < MinRepositionDistanceSquared)
            return;

        var newDiff = diff.Normalized() * guardian.DistanceAllowed;
        guardian.IsBeginRepositioned = true;
        try
        {
            _transform.SetLocalPosition(guardianUid, hostPos + newDiff, guardianXform);
        }
        finally
        {
            guardian.IsBeginRepositioned = false;
        }
    }

    /// <summary>
    /// Создаёт стража указанного прототипа внутри цели и делает её хозяином. Возвращает стража или null, если не получилось.
    /// </summary>
    public EntityUid? CreateGuardian(EntityUid target, string guardianProto)
    {
        if (HasComp<GuardianHostComponent>(target))
            return null;

        var hostXform = Transform(target);
        var host = EnsureComp<GuardianHostComponent>(target);
        var guardian = Spawn(guardianProto, _transform.GetMapCoordinates(target, xform: hostXform));

        _faction.IgnoreEntity(guardian, target);
        var sharedComp = EnsureComp<GuardianSharedComponent>(guardian);
        sharedComp.Host = target;
        Dirty(guardian, sharedComp);

        _container.Insert(guardian, host.GuardianContainer);
        host.HostedGuardian = guardian;

        if (TryComp<GuardianComponent>(guardian, out var guardianComp))
        {
            guardianComp.Host = target;
            _audio.PlayPvs(guardianComp.InjectSound, target);
            _popupSystem.PopupEntity(Loc.GetString("guardian-created"), target, target);
            return guardian;
        }

        QueueDel(guardian);
        QueueDel(host.ActionEntity);
        RemComp<GuardianHostComponent>(target);
        return null;
    }
}
