using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Botanism.Systems
{
    public class PlantExtractionSystem : ModSystem
    {
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

            ItemStack[] drops = block.GetDrops(sapi.World, blockSel.Position, byPlayer, 1f);

            if (drops == null || drops.Length == 0)
            {
                return;
            }

            // We are handling this ourselves now.
            // This prevents vanilla from also breaking/dropping the block.
            handling = EnumHandling.PreventDefault;

            // Remove the wild plant from the world.
            // Block id 0 is air.
            sapi.World.BlockAccessor.SetBlock(0, blockSel.Position);

            foreach (ItemStack drop in drops)
            {
                if (drop == null)
                {
                    continue;
                }

                // Temporary prototype behavior:
                // One wild plant becomes at least two plant items.
                drop.StackSize = Math.Max(2, drop.StackSize);

                bool addedToInventory = byPlayer.InventoryManager.TryGiveItemstack(drop, true);

                if (!addedToInventory)
                {
                    sapi.World.SpawnItemEntity(drop, blockSel.Position);
                }
            }

            Mod.Logger.Notification(
                "Botanism prototype extraction: {0} extracted from {1}",
                byPlayer.PlayerName,
                block.Code
            );
        }

        private static bool IsPropagationTool(IServerPlayer player)
        {
            EnumTool? activeTool = player.InventoryManager.ActiveTool;

            return activeTool == EnumTool.Knife || activeTool == EnumTool.Shears;
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