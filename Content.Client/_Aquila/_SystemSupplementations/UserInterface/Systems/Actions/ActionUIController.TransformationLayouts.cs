using System.Linq;
using Content.Client._Aquila.Actions.Types;
using Content.Shared._Aquila.Actions.Types;
using Content.Shared.Actions.Components;

namespace Content.Client.UserInterface.Systems.Actions;

public sealed partial class ActionUIController
{
    private static readonly TimeSpan TransformationLayoutGracePeriod = TimeSpan.FromSeconds(2);

    private TransformActionLayoutSnapshot? _transformLayout;
    private TransformActionLayoutSnapshot? _previousTransformLayout;
    private TransformActionLayoutSnapshot? _pendingTransformLayout;
    private NetEntity? _transformLayoutTarget;
    private EntityUid? _discardedTransformOwner;
    private TimeSpan _transformLayoutDeadline;

    private void ClearTransformationLayouts()
    {
        _transformLayout = null;
        _previousTransformLayout = null;
        _pendingTransformLayout = null;
        _transformLayoutTarget = null;
        _discardedTransformOwner = null;
    }

    private bool IsDiscardedTransformationBody(EntityUid uid)
    {
        return _discardedTransformOwner == uid;
    }

    private void CommitTransformationLayout()
    {
        _pendingTransformLayout = null;
        _transformLayoutTarget = null;
        _transformLayout = null;
        CacheTransformationLayout();
        if (_transformLayout != null)
            _transformLayout.UserModified = true;
    }

    private void UpdateTransformationLayout()
    {
        RestoreTransformationLayout();
        CacheTransformationLayout();
    }

    private void RetainTransformationAction(EntityUid action)
    {
        if (_transformLayout?.Slots.FirstOrDefault(slot => slot?.Entity == action) is { } saved)
            saved.RetainUntil = _timing.RealTime + TransformationLayoutGracePeriod;
    }

    private void CheckTransformationLayoutDeadline()
    {
        if (_pendingTransformLayout == null || _timing.RealTime < _transformLayoutDeadline)
            return;

        UpdateTransformationLayout();
        _pendingTransformLayout = null;
        _transformLayoutTarget = null;
        if (_actionsSystem != null)
            _container?.SetActionData(_actionsSystem, _actions.ToArray());
        QueueWindowUpdate();
    }

    private void CacheTransformationLayout()
    {
        if (_playerManager.LocalEntity is not { } uid)
            return;

        var layout = _transformLayout;
        if (layout == null || layout.Owner != uid)
        {
            if (layout != null)
                _previousTransformLayout = layout;

            _transformLayout = new TransformActionLayoutSnapshot
            {
                Owner = uid,
                Entity = EntityManager.GetNetEntity(uid),
                Slots = _actions.Select(action => DescribeTransformAction(uid, action)).ToList(),
            };
            return;
        }

        var slots = _actions.Select(action => DescribeTransformAction(uid, action)).ToList();
        for (var i = 0; i < layout.Slots.Count; i++)
        {
            var saved = layout.Slots[i];
            if (saved == null
                || saved.RetainUntil <= _timing.RealTime
                || slots.Any(slot => slot?.Action == saved.Action))
                continue;

            slots.Insert(Math.Min(i, slots.Count), saved);
        }
        layout.Slots = slots;
    }

    private TransformActionLayoutSlot? DescribeTransformAction(EntityUid uid, EntityUid? action)
    {
        if (action is not { } actionUid
            || !EntityManager.TryGetComponent<ActionComponent>(actionUid, out var component)
            || !EntityManager.TryGetComponent<MetaDataComponent>(actionUid, out var metadata))
            return null;

        return new TransformActionLayoutSlot
        {
            Action = EntityManager.GetNetEntity(actionUid),
            Entity = actionUid,
            Prototype = metadata.EntityPrototype is { } prototype ? new(prototype.ID) : null,
            Provider = EntityManager.GetNetEntity(component.Container),
            Personal = component.Container == null || component.Container == uid,
        };
    }

    /// <summary>
    /// Restores the cached action layout after a server-side body transformation.
    /// </summary>
    public void LoadTransformationLayout(TransformActionLayoutEvent args)
    {
        var snapshot = _transformLayout?.Entity == args.Source
            ? _transformLayout
            : _previousTransformLayout?.Entity == args.Source ? _previousTransformLayout : null;

        if (snapshot == null)
            return;

        _savedActions.Remove(snapshot.Owner);
        _discardedTransformOwner = snapshot.Owner;
        var current = _transformLayout;
        if (current != null && current.Entity == args.Target && current.UserModified)
            return;

        _pendingTransformLayout = snapshot;
        _transformLayoutTarget = args.Target;
        _transformLayoutDeadline = _timing.RealTime + TransformationLayoutGracePeriod;
        UpdateTransformationLayout();

        if (_actionsSystem != null)
            _container?.SetActionData(_actionsSystem, _actions.ToArray());
        QueueWindowUpdate();
    }

    private void RestoreTransformationLayout()
    {
        if (_pendingTransformLayout == null
            || _playerManager.LocalEntity is not { } uid)
            return;

        var localEntity = EntityManager.GetNetEntity(uid);
        if (localEntity != _transformLayoutTarget)
        {
            if (localEntity != _pendingTransformLayout.Entity)
            {
                _pendingTransformLayout = null;
                _transformLayoutTarget = null;
            }
            return;
        }

        var available = new List<(EntityUid Entity, TransformActionLayoutSlot Slot)>();
        foreach (var action in _actions)
        {
            if (action is { } actionUid && DescribeTransformAction(uid, action) is { } slot)
                available.Add((actionUid, slot));
        }

        var used = new HashSet<EntityUid>();
        var restored = new List<EntityUid?>();
        var complete = true;

        foreach (var saved in _pendingTransformLayout.Slots)
        {
            if (saved == null)
            {
                restored.Add(null);
                continue;
            }

            var match = available.FindIndex(candidate => !used.Contains(candidate.Entity)
                && candidate.Slot.Action == saved.Action);
            if (match < 0 && saved.Prototype != null)
            {
                match = available.FindIndex(candidate => !used.Contains(candidate.Entity)
                    && candidate.Slot.Prototype == saved.Prototype
                    && (candidate.Slot.Personal && saved.Personal
                        || candidate.Slot.Provider == saved.Provider));
            }

            if (match < 0)
            {
                complete = false;
                continue;
            }

            var action = available[match].Entity;
            used.Add(action);
            restored.Add(action);
        }

        restored.AddRange(_actions.Where(action => action is { } actionUid && !used.Contains(actionUid)));
        _actions = restored;
        _transformLayout = null;

        if (complete)
        {
            _pendingTransformLayout = null;
            _transformLayoutTarget = null;
        }
    }
}
