using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Botanism
{
    public class BotanismModSystem : ModSystem
    {
        private ICoreServerAPI sapi;

        // Called on server and client
        public override void Start(ICoreAPI api)
        {
            Mod.Logger.Notification("Botanism loaded");
            Mod.Logger.Notification("Botanism language test: " + Lang.Get("botanism:hello"));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;

            sapi.Event.BreakBlock += OnBreakBlock;

            Mod.Logger.Notification("Botanism server systems initialized");
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Botanism client systems initialized");
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

            if (!IsPrototypeFlower(block))
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

            // Remove the wild flower from the world.
            // Block id 0 is air.
            sapi.World.BlockAccessor.SetBlock(0, blockSel.Position);

            foreach (ItemStack drop in drops)
            {
                if (drop == null)
                {
                    continue;
                }

                // Temporary prototype behavior:
                // One wild flower becomes at least two flower items.
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

        private static bool IsPrototypeFlower(Block block)
        {
            string path = block.Code?.Path ?? "";

            // Current prototype target:
            // vanilla free-standing flowers like game:flower-cornflower-free
            return block.Code?.Domain == "game"
                && path.StartsWith("flower-")
                && path.EndsWith("-free");
        }
    }
}