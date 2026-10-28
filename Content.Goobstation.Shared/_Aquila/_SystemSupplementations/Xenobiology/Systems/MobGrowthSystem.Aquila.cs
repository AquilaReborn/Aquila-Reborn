using Content.Goobstation.Shared.Xenobiology.Components;

namespace Content.Goobstation.Shared.Xenobiology.Systems;

public sealed partial class MobGrowthSystem
{
    private void UpdateGrowthName(Entity<MobGrowthComponent> ent, GrowthStageData stageData)
    {
        if (!_net.IsServer)
            return;

        var name = string.IsNullOrEmpty(stageData.DisplayName)
            ? ent.Comp.BaseEntityName
            : Loc.GetString("mob-growth-stage-name", ("stage", Loc.GetString(stageData.DisplayName)), ("name", ent.Comp.BaseEntityName));
        _metaData.SetEntityName(ent, name);
    }
}
