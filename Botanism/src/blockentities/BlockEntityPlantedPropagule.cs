using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Botanism.BlockEntities;

public class BlockEntityPlantedPropagule : BlockEntity
{
    public string ProfileCode { get; private set; } = "";
    public string PlantDisplayName { get; private set; } = "";
    public string TargetPlantCode { get; private set; } = "";
    public string PropagationType { get; private set; } = "generic";
    public string PlacementType { get; private set; } = "surface";
    public double PlantedAtTotalDays { get; private set; }

    public void InitializeFromPropagule(ItemStack stack, double plantedAtTotalDays)
    {
        ProfileCode = stack.Attributes.GetString("profileCode", "");
        PlantDisplayName = stack.Attributes.GetString("plantDisplayName", "");
        TargetPlantCode = stack.Attributes.GetString("targetPlantCode", "");
        PropagationType = stack.Attributes.GetString("propagationType", "generic");
        PlacementType = stack.Attributes.GetString("placementType", "surface");
        PlantedAtTotalDays = plantedAtTotalDays;

        MarkDirty(true);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        tree.SetString("profileCode", ProfileCode);
        tree.SetString("plantDisplayName", PlantDisplayName);
        tree.SetString("targetPlantCode", TargetPlantCode);
        tree.SetString("propagationType", PropagationType);
        tree.SetString("placementType", PlacementType);
        tree.SetDouble("plantedAtTotalDays", PlantedAtTotalDays);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);

        ProfileCode = tree.GetString("profileCode", "");
        PlantDisplayName = tree.GetString("plantDisplayName", "");
        TargetPlantCode = tree.GetString("targetPlantCode", "");
        PropagationType = tree.GetString("propagationType", "generic");
        PlacementType = tree.GetString("placementType", "surface");
        PlantedAtTotalDays = tree.GetDouble("plantedAtTotalDays", 0);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);

        if (!string.IsNullOrWhiteSpace(PlantDisplayName))
        {
            dsc.AppendLine(Lang.Get("botanism:block-plantedpropagule-source", PlantDisplayName));
        }
    }
}