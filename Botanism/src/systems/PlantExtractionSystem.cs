using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Botanism.Systems
{
    public class PlantExtractionSystem : ModSystem
    {
        private const int PrototypeYield = 2;

        private static readonly AssetLocation PropaguleItemCode = new AssetLocation("botanism", "propagule");

        private ICoreServerAPI sapi;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;

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

            if (!IsPropagationTool(byPlayer))
            {
                return;
            }

            Block block = blockSel.Block;

            if (!IsPrototypePlant(block))
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

            // We are handling this ourselves now.
            // This prevents vanilla from also breaking/dropping the block.
            handling = EnumHandling.PreventDefault;

            // Remove the wild plant from the world.
            // Block id 0 is air.
            sapi.World.BlockAccessor.SetBlock(0, blockSel.Position);

            ItemStack propaguleStack = new ItemStack(propaguleItem, PrototypeYield);

            // Temporary metadata for the future profile/growth system.
            // The item name is still generic for now.
            propaguleStack.Attributes.SetString("sourcePlantCode", block.Code.ToString());
            propaguleStack.Attributes.SetString("targetPlantCode", block.Code.ToString());
            propaguleStack.Attributes.SetString("propagationType", "generic");

            bool addedToInventory = byPlayer.InventoryManager.TryGiveItemstack(propaguleStack, true);

            if (!addedToInventory)
            {
                sapi.World.SpawnItemEntity(propaguleStack, blockSel.Position);
            }

            Mod.Logger.Notification(
                "Botanism prototype extraction: {0} extracted {1} from {2}",
                byPlayer.PlayerName,
                PropaguleItemCode,
                block.Code
            );
        }

        private static bool IsPropagationTool(IServerPlayer player)
        {
            EnumTool? activeTool = player.InventoryManager.ActiveTool;

            return activeTool == EnumTool.Knife;
        }

        private static bool IsPrototypePlant(Block block)
        {
            string path = block.Code?.Path ?? "";

            // Current prototype target:
            // vanilla free-standing plant blocks like game:flower-cornflower-free and game:flower-horsetail-free
            return block.Code?.Domain == "game"
                && path.StartsWith("flower-")
                && path.EndsWith("-free");
        }
    }
}