// SPDX-License-Identifier: MIT

using Content.Shared._Aquila.PreCritical;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Client.UserInterface.Systems.DamageOverlays.Overlays;

public sealed partial class DamageOverlay
{
    private const float PreCriticalDarkeningLevel = 0.14f;
    private const float PreCriticalFadeTolerance = 0.001f;

    private EntityUid? _preCriticalOwner;
    private float _preCriticalDarkening;

    private float GetDarkeningLevel(float frameTime)
    {
        var entity = _playerManager.LocalEntity;
        if (_preCriticalOwner != entity)
        {
            _preCriticalOwner = entity;
            _preCriticalDarkening = 0f;
        }

        if (State != MobState.Alive ||
            !_entityManager.TryGetComponent(entity, out MobStateComponent? mobState) ||
            mobState.CurrentState != MobState.Alive ||
            !_entityManager.TryGetComponent(entity, out MobThresholdsComponent? thresholds) ||
            !thresholds.ShowOverlays)
        {
            _preCriticalDarkening = 0f;
            return State == MobState.Critical ? 1f : _oldOxygenLevel;
        }

        var target = _entityManager.HasComponent<PreCriticalActiveComponent>(entity)
            ? PreCriticalDarkeningLevel
            : 0f;
        var difference = target - _preCriticalDarkening;
        _preCriticalDarkening = MathF.Abs(difference) <= PreCriticalFadeTolerance
            ? target
            : _preCriticalDarkening + GetDiff(difference, frameTime);

        return MathF.Max(_oldOxygenLevel, _preCriticalDarkening);
    }
}
