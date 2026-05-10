using Botanism.Profiles;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Botanism.Systems
{
    public class PlantExtractionSystem : ModSystem
    {
        private static readonly AssetLocation PropaguleItemCode = new AssetLocation("botanism", "propagule");

        private ICoreServerAPI sapi;
        private PlantProfileSystem plantProfiles;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            plantProfiles = api.ModLoader.GetModSystem<PlantProfileSystem>();

            sapi.Event.BreakBlock += OnBreakBlock;

            Mod.Logger.Notification("Botanism plant extraction system initialized");
        }

        public override void Dispose()
        {
            if (sapi != null)
            {
                sapi.Event.BreakBlock -= OnBreakBlock;
            }
        }

        private void OnBreakBlock(
            IServerPlayer byPlayer,
            BlockSelection blockSel,
            ref float dropQuantityMultiplier,
            ref EnumHandling handling
        )
        {
            if (byPlayer == null || blockSel == null || blockSel.Block == null)
            {
                return;
            }

            if (plantProfiles == null)
            {
                return;
            }

            Block block = blockSel.Block;
            PlantProfile profile = plantProfiles.GetProfileForBlock(block.Code);

            if (profile == null)
            {
                return;
            }

            if (!IsPropagationTool(byPlayer, profile))
            {
                return;
            }

            Item propaguleItem = sapi.World.GetItem(PropaguleItemCode);

            if (propaguleItem == null)
            {
                Mod.Logger.Warning(
                    "Botanism could not find item {0}. Plant extraction was skipped.",
                    PropaguleItemCode
                );

                return;
            }

            handling = EnumHandling.PreventDefault;

            // Remove the wild plant from the world.
            // Block id 0 is air.
            sapi.World.BlockAccessor.SetBlock(0, blockSel.Position);

            ItemStack propaguleStack = new ItemStack(propaguleItem, profile.Yield);

            string targetBlockCode = string.IsNullOrWhiteSpace(profile.TargetBlockCode)
                ? block.Code.ToString()
                : profile.TargetBlockCode;

            propaguleStack.Attributes.SetString("profileCode", profile.Code);
            propaguleStack.Attributes.SetString("plantDisplayName", profile.DisplayName);
            propaguleStack.Attributes.SetString("plantCategory", profile.PlantCategory);
            propaguleStack.Attributes.SetString("sourcePlantCode", block.Code.ToString());
            propaguleStack.Attributes.SetString("targetPlantCode", targetBlockCode);
            propaguleStack.Attributes.SetString("propagationType", profile.PropagationType);
            propaguleStack.Attributes.SetString("placementType", profile.PlacementType);

            bool addedToInventory = byPlayer.InventoryManager.TryGiveItemstack(propaguleStack, true);

            if (!addedToInventory)
            {
                sapi.World.SpawnItemEntity(propaguleStack, blockSel.Position);
            }

            Mod.Logger.Notification(
                "Botanism extraction: {0} extracted {1} {2} from {3}",
                byPlayer.PlayerName,
                profile.Yield,
                profile.Code,
                block.Code
            );
        }

        private static bool IsPropagationTool(IServerPlayer player, PlantProfile profile)
        {
            string activeToolCode = GetActiveToolCode(player);

            return profile.AllowsTool(activeToolCode);
        }

        private static string GetActiveToolCode(IServerPlayer player)
        {
            EnumTool? activeTool = player.InventoryManager.ActiveTool;

            return activeTool switch
            {
                EnumTool.Knife => "knife",
                EnumTool.Shears => "shears",
                _ => ""
            };
        }
    }
}