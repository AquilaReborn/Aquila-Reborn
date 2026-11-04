using Content.Server._Aquila.Cyberware.Components;
using Content.Server.Administration.Logs;
using Content.Shared._Aquila.Cyberware;
using Content.Shared._Aquila.Cyberware.Components;
using Content.Shared._Aquila.Cyberware.DataDefinitions;
using Content.Shared._Aquila.Cyberware.Prototypes;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Cyberware;

/// <summary>
/// Ведёт стадии киберпсихоза по риску кибернагрузки и вызывает приступы.
/// </summary>
public sealed class CyberpsychosisSystem : SharedCyberpsychosisSystem
{
    private static readonly LocId SuppressedMessage = "cyberpsychosis-suppressed";
    private static readonly LocId SuppressionEndedMessage = "cyberpsychosis-suppression-ended";
    private static readonly LocId TherapyBlockedMessage = "cyberpsychosis-therapy-blocked";
    private static readonly LocId CuredMessage = "cyberpsychosis-cured";

    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly CyberLoadSystem _cyberLoad = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberpsychosisComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CyberpsychosisComponent, ExaminedEvent>(OnExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var loadQuery = EntityQueryEnumerator<CyberLoadComponent>();
        while (loadQuery.MoveNext(out var uid, out var load))
        {
            if (now < load.NextStageCheck)
                continue;

            load.NextStageCheck = now + load.StageCheckInterval;
            CheckStage((uid, load));
        }

        var query = EntityQueryEnumerator<CyberpsychosisComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateTreatment((uid, comp), now, frameTime);

            if (now < comp.NextEpisode
                || string.IsNullOrEmpty(comp.Stage.Id)
                || !_proto.TryIndex(comp.Stage, out var stage))
                continue;

            comp.NextEpisode = now + _random.Next(stage.MinEpisodeDelay, stage.MaxEpisodeDelay);

            if (!comp.Suppressed
                && _mobState.IsAlive(uid)
                && PickEpisode(stage) is { } episode)
                _effects.ApplyEffects(uid, episode.Effects);
        }
    }

    /// <summary>
    /// Глушит приступы на время и обрывает начавшийся приступ ярости.
    /// </summary>
    public void Suppress(Entity<CyberpsychosisComponent?> ent, TimeSpan time)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var until = _timing.CurTime + time;
        if (until > ent.Comp.SuppressedUntil)
            ent.Comp.SuppressedUntil = until;

        if (ent.Comp.Suppressed)
            return;

        ent.Comp.Suppressed = true;
        Dirty(ent.Owner, ent.Comp);
        RemCompDeferred<CyberpsychosisRampageComponent>(ent);
        ShowMessage(ent, SuppressedMessage);
    }

    /// <summary>
    /// Продвигает курс лечения. Курс идёт, только пока риск кибернагрузки ниже порога.
    /// </summary>
    public void ApplyTherapy(Entity<CyberpsychosisComponent?> ent, float seconds)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var now = _timing.CurTime;
        if (TryComp<CyberLoadComponent>(ent, out var load)
            && _cyberLoad.GetRisk((ent.Owner, load)) >= load.Threshold)
        {
            if (now < ent.Comp.NextTherapyWarning)
                return;

            ent.Comp.NextTherapyWarning = now + ent.Comp.TherapyWarningDelay;
            ShowMessage(ent, TherapyBlockedMessage);
            return;
        }

        ent.Comp.LastTherapy = now;
        ent.Comp.CureProgress += seconds;

        if (ent.Comp.CureProgress >= ent.Comp.CureTime)
            Cure(ent);
    }

    /// <summary>
    /// Полностью снимает киберпсихоз на любой стадии.
    /// </summary>
    public void Cure(EntityUid uid)
    {
        if (!HasComp<CyberpsychosisComponent>(uid))
            return;

        RemCompDeferred<CyberpsychosisComponent>(uid);
        RemCompDeferred<CyberpsychosisRampageComponent>(uid);
        ShowMessage(uid, CuredMessage);
        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(uid):player} was cured of cyberpsychosis");

        if (TryComp<CyberLoadComponent>(uid, out var load))
            load.NextStageCheck = _timing.CurTime + load.CureGrace;
    }

    private void UpdateTreatment(Entity<CyberpsychosisComponent> ent, TimeSpan now, float frameTime)
    {
        if (ent.Comp.Suppressed && now >= ent.Comp.SuppressedUntil)
        {
            ent.Comp.Suppressed = false;
            Dirty(ent);
            ShowMessage(ent, SuppressionEndedMessage);
        }

        if (ent.Comp.CureProgress > 0f && now - ent.Comp.LastTherapy > ent.Comp.TherapyGrace)
            ent.Comp.CureProgress = MathF.Max(0f, ent.Comp.CureProgress - ent.Comp.CureDecay * frameTime);
    }

    private void OnShutdown(Entity<CyberpsychosisComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        RemoveStageComponents(ent);
    }

    private void OnExamined(Entity<CyberpsychosisComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange
            || string.IsNullOrEmpty(ent.Comp.Stage.Id)
            || !_proto.TryIndex(ent.Comp.Stage, out var stage)
            || stage.ExamineMessage is not { } message)
            return;

        args.PushMarkup(Loc.GetString(message, ("target", Identity.Entity(ent, EntityManager))));
    }

    private void CheckStage(Entity<CyberLoadComponent> ent)
    {
        if (!_mobState.IsAlive(ent))
            return;

        var level = GetLevel(ent);
        var risk = _cyberLoad.GetRisk(ent.AsNullable());

        if (level > 0
            && _proto.Index(ent.Comp.Stages[level - 1]) is { Reversible: true } current
            && risk < current.MinRisk)
        {
            SetLevel(ent, level - 1);
            return;
        }

        if (level >= ent.Comp.Stages.Count
            || TryComp<CyberpsychosisComponent>(ent, out var comp) && comp.Suppressed)
            return;

        var next = _proto.Index(ent.Comp.Stages[level]);
        if (risk < next.MinRisk)
            return;

        var chance = next.MinRisk > 0f ? next.Chance * risk / next.MinRisk : next.Chance;
        var ev = new GetCyberpsychosisChanceEvent(1f);
        RaiseLocalEvent(ent, ref ev);

        if (_random.Prob(Math.Clamp(chance * ev.Multiplier, 0f, 1f)))
            SetLevel(ent, level + 1);
    }

    private int GetLevel(Entity<CyberLoadComponent> ent)
    {
        if (!TryComp<CyberpsychosisComponent>(ent, out var comp))
            return 0;

        return ent.Comp.Stages.IndexOf(comp.Stage) + 1;
    }

    private void SetLevel(Entity<CyberLoadComponent> ent, int level)
    {
        var previous = GetLevel(ent);
        TryComp<CyberpsychosisComponent>(ent, out var comp);
        var previousStage = comp != null ? _proto.Index(comp.Stage) : null;

        if (level <= 0)
        {
            ShowMessage(ent, previousStage?.ExitMessage);
            RemComp<CyberpsychosisComponent>(ent);
            return;
        }

        var stageId = ent.Comp.Stages[level - 1];
        var stage = _proto.Index(stageId);

        comp ??= EnsureComp<CyberpsychosisComponent>(ent);
        RemoveStageComponents((ent, comp));

        comp.Stage = stageId;
        comp.NextEpisode = _timing.CurTime + _random.Next(stage.MinEpisodeDelay, stage.MaxEpisodeDelay);
        AddStageComponents((ent, comp), stage);
        Dirty(ent, comp);

        ShowMessage(ent, level > previous ? stage.EnterMessage : previousStage?.ExitMessage);
    }

    private void AddStageComponents(Entity<CyberpsychosisComponent> ent, CyberpsychosisStagePrototype stage)
    {
        var toAdd = new ComponentRegistry();
        foreach (var (name, entry) in stage.Components)
        {
            if (HasComp(ent.Owner, entry.Component.GetType()))
                continue;

            toAdd.Add(name, entry);
            ent.Comp.AddedComponents.Add(name);
        }

        EntityManager.AddComponents(ent.Owner, toAdd);
    }

    private void RemoveStageComponents(Entity<CyberpsychosisComponent> ent)
    {
        foreach (var name in ent.Comp.AddedComponents)
        {
            if (_factory.TryGetRegistration(name, out var registration))
                RemComp(ent.Owner, registration.Type);
        }

        ent.Comp.AddedComponents.Clear();
    }

    private CyberpsychosisEpisode? PickEpisode(CyberpsychosisStagePrototype stage)
    {
        var total = 0f;
        foreach (var episode in stage.Episodes)
        {
            total += episode.Weight;
        }

        if (total <= 0f)
            return null;

        var roll = _random.NextFloat(total);
        foreach (var episode in stage.Episodes)
        {
            roll -= episode.Weight;
            if (roll <= 0f)
                return episode;
        }

        return stage.Episodes[^1];
    }

    private void ShowMessage(EntityUid uid, LocId? message)
    {
        if (message is { } id)
            _popup.PopupEntity(Loc.GetString(id), uid, uid, PopupType.MediumCaution);
    }
}
