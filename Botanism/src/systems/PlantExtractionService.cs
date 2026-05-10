using System;
using System.Collections.Generic;
using Botanism.Profiles;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Botanism.Systems
{
    public class PlantExtractionService : ModSystem
    {
        private static readonly AssetLocation PropaguleItemCode = new AssetLocation("botanism", "propagule");
        private const int ExtractionToolDamage = 2;

        private readonly Dictionary<string, ItemStack[]> toolStacksByToolCode =
            new Dictionary<string, ItemStack[]>(StringComparer.OrdinalIgnoreCase);

        private ICoreAPI api;
        private PlantProfileSystem plantProfiles;

        public override void Start(ICoreAPI api)
        {
            this.api = api;
            plantProfiles = api.ModLoader.GetModSystem<PlantProfileSystem>();
        }

        public PlantProfile GetProfileForSelection(IWorldAccessor world, BlockSelection blockSel)
        {
            Block block = GetSelectedBlock(world, blockSel);

            if (block?.Code == null || plantProfiles == null)
            {
                return null;
            }

            return plantProfiles.GetProfileForBlock(block.Code);
        }

        public bool CanExtractPlant(
            IWorldAccessor world,
            IPlayer player,
            BlockSelection blockSel,
            out PlantProfile profile
        )
        {
            profile = GetProfileForSelection(world, blockSel);

            if (profile == null)
            {
                return false;
            }

            string activeToolCode = GetActiveToolCode(player);

            return profile.AllowsTool(activeToolCode);
        }

        public bool TryExtractPlant(IWorldAccessor world, IPlayer player, BlockSelection blockSel)
        {
            if (!CanExtractPlant(world, player, blockSel, out PlantProfile profile))
            {
                return false;
            }

            // Client side only needs to acknowledge that the interaction is valid.
            // The actual block removal, tool damage, and item drops must happen server side.
            if (world.Side != EnumAppSide.Server)
            {
                return true;
            }

            Block sourceBlock = GetSelectedBlock(world, blockSel);

            if (sourceBlock?.Code == null)
            {
                return false;
            }

            Item propaguleItem = world.GetItem(PropaguleItemCode);

            if (propaguleItem == null)
            {
                Mod.Logger.Warning(
                    "Botanism could not find item {0}. Plant extraction was skipped.",
                    PropaguleItemCode
                );

                return false;
            }

            int yield = profile.Yield;

            if (yield <= 0)
            {
                Mod.Logger.Warning(
                    "Botanism plant profile '{0}' has a yield of {1}. No propagules were dropped.",
                    profile.Code,
                    yield
                );

                return false;
            }

            world.BlockAccessor.SetBlock(0, blockSel.Position);

            ItemStack propaguleStack = CreatePropaguleStack(propaguleItem, profile, sourceBlock);

            SpawnPropaguleDrop(world, blockSel.Position, propaguleStack, yield);
            DamageActiveTool(world, player);

            Mod.Logger.Notification(
                "Botanism extraction: {0} extracted {1} {2} from {3}",
                player?.PlayerName ?? "Unknown player",
                yield,
                profile.Code,
                sourceBlock.Code
            );

            return true;
        }

        public ItemStack[] GetToolStacksForProfile(IWorldAccessor world, PlantProfile profile)
        {
            if (profile == null)
            {
                return Array.Empty<ItemStack>();
            }

            List<ItemStack> toolStacks = new List<ItemStack>();

            foreach (string toolCode in GetValidToolCodes(profile))
            {
                toolStacks.AddRange(GetToolStacksForToolCode(world, toolCode));
            }

            return toolStacks.ToArray();
        }

        private ItemStack[] GetToolStacksForToolCode(IWorldAccessor world, string toolCode)
        {
            if (string.IsNullOrWhiteSpace(toolCode))
            {
                return Array.Empty<ItemStack>();
            }

            if (toolStacksByToolCode.TryGetValue(toolCode, out ItemStack[] cachedStacks))
            {
                return cachedStacks;
            }

            if (!TryGetTool(toolCode, out EnumTool tool))
            {
                toolStacksByToolCode[toolCode] = Array.Empty<ItemStack>();
                return Array.Empty<ItemStack>();
            }

            List<ItemStack> stacks = new List<ItemStack>();

            foreach (Item item in world.Items)
            {
                if (item?.Code == null)
                {
                    continue;
                }

                if (item.Tool == tool)
                {
                    stacks.Add(new ItemStack(item));
                }
            }

            ItemStack[] result = stacks.ToArray();
            toolStacksByToolCode[toolCode] = result;

            return result;
        }

        private static IEnumerable<string> GetValidToolCodes(PlantProfile profile)
        {
            if (profile.ValidTools == null || profile.ValidTools.Length == 0)
            {
                yield return "knife";
                yield break;
            }

            foreach (string toolCode in profile.ValidTools)
            {
                if (!string.IsNullOrWhiteSpace(toolCode))
                {
                    yield return toolCode;
                }
            }
        }

        private static bool TryGetTool(string toolCode, out EnumTool tool)
        {
            return Enum.TryParse(toolCode, true, out tool);
        }

        private static string GetActiveToolCode(IPlayer player)
        {
            EnumTool? activeTool = player?.InventoryManager?.ActiveTool;

            return activeTool switch
            {
                EnumTool.Knife => "knife",
                EnumTool.Shears => "shears",
                _ => activeTool?.ToString() ?? ""
            };
        }

        private static Block GetSelectedBlock(IWorldAccessor world, BlockSelection blockSel)
        {
            if (world == null || blockSel == null)
            {
                return null;
            }

            if (blockSel.Block?.Code != null)
            {
                return blockSel.Block;
            }

            return world.BlockAccessor.GetBlock(blockSel.Position);
        }

        private static ItemStack CreatePropaguleStack(Item propaguleItem, PlantProfile profile, Block sourceBlock)
        {
            ItemStack propaguleStack = new ItemStack(propaguleItem, 1);

            string targetBlockCode = string.IsNullOrWhiteSpace(profile.TargetBlockCode)
                ? sourceBlock.Code.ToString()
                : profile.TargetBlockCode;

            propaguleStack.Attributes.SetString("profileCode", profile.Code);
            propaguleStack.Attributes.SetString("plantDisplayName", profile.DisplayName);
            propaguleStack.Attributes.SetString("plantCategory", profile.PlantCategory);
            propaguleStack.Attributes.SetString("sourcePlantCode", sourceBlock.Code.ToString());
            propaguleStack.Attributes.SetString("targetPlantCode", targetBlockCode);
            propaguleStack.Attributes.SetString("propagationType", profile.PropagationType);
            propaguleStack.Attributes.SetString("placementType", profile.PlacementType);

            return propaguleStack;
        }

        private static void SpawnPropaguleDrop(
            IWorldAccessor world,
            BlockPos position,
            ItemStack propaguleStack,
            int quantity
        )
        {
            ItemStack dropStack = propaguleStack.Clone();
            dropStack.StackSize = quantity;

            Vec3d dropPosition = new Vec3d(
                position.X + 0.5 + (world.Rand.NextDouble() - 0.5) * 0.35,
                position.Y + 0.15,
                position.Z + 0.5 + (world.Rand.NextDouble() - 0.5) * 0.35
            );

            Vec3d dropVelocity = new Vec3d(
                (world.Rand.NextDouble() - 0.5) * 0.03,
                0.03,
                (world.Rand.NextDouble() - 0.5) * 0.03
            );

            world.SpawnItemEntity(dropStack, dropPosition, dropVelocity);
        }

        private static void DamageActiveTool(IWorldAccessor world, IPlayer player)
        {
            ItemSlot activeSlot = player?.InventoryManager?.ActiveHotbarSlot;

            if (activeSlot?.Itemstack?.Collectible == null)
            {
                return;
            }

            activeSlot.Itemstack.Collectible.DamageItem(
                world,
                player.Entity,
                activeSlot,
                ExtractionToolDamage
            );

            activeSlot.MarkDirty();
        }
    }
}