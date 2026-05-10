using Botanism.BlockEntities;
using System;
using System.Globalization;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Botanism.Items
{
    public class ItemPropagule : Item
    {
        public override string GetHeldItemName(ItemStack itemStack)
        {
            string plantName = itemStack?.Attributes?.GetString("plantDisplayName");

            if (string.IsNullOrWhiteSpace(plantName))
            {
                string targetPlantCode = itemStack?.Attributes?.GetString("targetPlantCode");

                if (!string.IsNullOrWhiteSpace(targetPlantCode))
                {
                    plantName = GetPlantDisplayName(targetPlantCode);
                }
            }

            if (string.IsNullOrWhiteSpace(plantName))
            {
                return base.GetHeldItemName(itemStack);
            }

            string propagationType = itemStack.Attributes.GetString("propagationType", "generic");
            string materialName = GetPropagationMaterialDisplayName(propagationType);

            return Lang.Get("botanism:item-propagule-named-material", plantName, materialName);
        }

        public override void GetHeldItemInfo(
            ItemSlot inSlot,
            StringBuilder dsc,
            IWorldAccessor world,
            bool withDebugInfo
        )
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            ItemStack itemStack = inSlot?.Itemstack;

            if (itemStack == null)
            {
                return;
            }

            string plantName = itemStack.Attributes.GetString("plantDisplayName");
            string targetPlantCode = itemStack.Attributes.GetString("targetPlantCode");
            string propagationType = itemStack.Attributes.GetString("propagationType", "generic");

            if (string.IsNullOrWhiteSpace(plantName) && !string.IsNullOrWhiteSpace(targetPlantCode))
            {
                plantName = GetPlantDisplayName(targetPlantCode);
            }

            if (!string.IsNullOrWhiteSpace(plantName))
            {
                dsc.AppendLine(Lang.Get("botanism:item-propagule-source", plantName));
            }

            dsc.AppendLine(Lang.Get(
                "botanism:item-propagule-type",
                GetPropagationTypeDisplayName(propagationType)
            ));
        }

        private static string GetPlantDisplayName(string plantCode)
        {
            string path = plantCode;

            int domainSeparatorIndex = path.IndexOf(':');

            if (domainSeparatorIndex >= 0 && domainSeparatorIndex < path.Length - 1)
            {
                path = path.Substring(domainSeparatorIndex + 1);
            }

            if (path.StartsWith("flower-"))
            {
                path = path.Substring("flower-".Length);
            }

            if (path.EndsWith("-free"))
            {
                path = path.Substring(0, path.Length - "-free".Length);
            }

            if (path.EndsWith("-snow"))
            {
                path = path.Substring(0, path.Length - "-snow".Length);
            }

            path = path.Replace("-", " ");

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(path);
        }

        private static string GetPropagationTypeDisplayName(string propagationType)
        {
            return propagationType switch
            {
                "seed" => Lang.Get("botanism:propagation-type-seed"),
                "bulb" => Lang.Get("botanism:propagation-type-bulb"),
                "spore" => Lang.Get("botanism:propagation-type-spore"),
                "rhizome" => Lang.Get("botanism:propagation-type-rhizome"),
                "pip" => Lang.Get("botanism:propagation-type-pip"),
                "cutting" => Lang.Get("botanism:propagation-type-cutting"),
                "division" => Lang.Get("botanism:propagation-type-division"),
                "fragment" => Lang.Get("botanism:propagation-type-fragment"),
                _ => Lang.Get("botanism:propagation-type-generic")
            };
        }

        private static string GetPropagationMaterialDisplayName(string propagationType)
        {
            return propagationType switch
            {
                "seed" => Lang.Get("botanism:propagule-material-seed"),
                "bulb" => Lang.Get("botanism:propagule-material-bulb"),
                "spore" => Lang.Get("botanism:propagule-material-spore"),
                "rhizome" => Lang.Get("botanism:propagule-material-rhizome"),
                "pip" => Lang.Get("botanism:propagule-material-pip"),
                "cutting" => Lang.Get("botanism:propagule-material-cutting"),
                "division" => Lang.Get("botanism:propagule-material-division"),
                "fragment" => Lang.Get("botanism:propagule-material-fragment"),
                _ => Lang.Get("botanism:propagule-material-generic")
            };
        }

        private static readonly AssetLocation PlantedPropaguleBlockCode = new("botanism", "plantedpropagule");

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            WorldInteraction[] baseInteractions = base.GetHeldInteractionHelp(inSlot) ?? Array.Empty<WorldInteraction>();

            string targetPlantCode = inSlot?.Itemstack?.Attributes?.GetString("targetPlantCode", "") ?? "";

            if (string.IsNullOrWhiteSpace(targetPlantCode))
            {
                return baseInteractions;
            }

            WorldInteraction plantInteraction = new()
            {
                ActionLangCode = "botanism:heldhelp-plant",
                MouseButton = EnumMouseButton.Right
            };

            WorldInteraction[] interactions = new WorldInteraction[baseInteractions.Length + 1];

            interactions[0] = plantInteraction;
            baseInteractions.CopyTo(interactions, 1);

            return interactions;
        }

        public override void OnHeldInteractStart(
            ItemSlot slot,
            EntityAgent byEntity,
            BlockSelection blockSel,
            EntitySelection entitySel,
            bool firstEvent,
            ref EnumHandHandling handling
        )
        {
            if (!firstEvent || slot?.Itemstack == null || blockSel == null)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }

            // Keep Ctrl + right-click available for GroundStorable bag placement.
            if (byEntity.Controls.CtrlKey)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }

            if (!CanPlantPropagule(slot, byEntity, blockSel, out BlockPos placePos))
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }

            // Only prevent default once we know this is actually a planting action.
            handling = EnumHandHandling.PreventDefault;

            if (byEntity.World.Side != EnumAppSide.Server)
            {
                return;
            }

            PlantPropagule(slot, byEntity, placePos);
        }

        private static bool CanPlantPropagule(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, out BlockPos placePos)
        {
            placePos = null;

            string targetPlantCode = slot.Itemstack.Attributes.GetString("targetPlantCode", "");

            if (string.IsNullOrWhiteSpace(targetPlantCode))
            {
                return false;
            }

            if (blockSel.Face != BlockFacing.UP)
            {
                return false;
            }

            IWorldAccessor world = byEntity.World;

            BlockPos candidatePlacePos = blockSel.Position.UpCopy();

            Block existingBlock = world.BlockAccessor.GetBlock(candidatePlacePos);

            if (existingBlock?.Id != 0)
            {
                return false;
            }

            Block groundBlock = world.BlockAccessor.GetBlock(blockSel.Position);

            if (!IsBasicPlantableSurface(groundBlock))
            {
                return false;
            }

            placePos = candidatePlacePos;
            return true;
        }

        private static void PlantPropagule(ItemSlot slot, EntityAgent byEntity, BlockPos placePos)
        {
            IWorldAccessor world = byEntity.World;

            Block plantedBlock = world.BlockAccessor.GetBlock(PlantedPropaguleBlockCode);

            if (plantedBlock == null || plantedBlock.Id == 0)
            {
                world.Logger.Warning(
                    "Botanism could not find block '{0}'. Propagule planting was skipped.",
                    PlantedPropaguleBlockCode
                );

                return;
            }

            world.BlockAccessor.SetBlock(plantedBlock.BlockId, placePos);

            BlockEntityPlantedPropagule blockEntity =
                world.BlockAccessor.GetBlockEntity<BlockEntityPlantedPropagule>(placePos);

            if (blockEntity == null)
            {
                world.Logger.Warning(
                    "Botanism placed '{0}' at {1}, but no PlantedPropagule block entity was created. Removing block.",
                    PlantedPropaguleBlockCode,
                    placePos
                );

                world.BlockAccessor.SetBlock(0, placePos);
                return;
            }

            blockEntity.InitializeFromPropagule(slot.Itemstack, world.Calendar.TotalDays);

            slot.TakeOut(1);
            slot.MarkDirty();
        }

        private static bool IsBasicPlantableSurface(Block block)
        {
            if (block?.Code == null)
            {
                return false;
            }

            string blockCode = block.Code.ToString();

            return
                blockCode.StartsWith("game:soil-", StringComparison.OrdinalIgnoreCase) ||
                blockCode.StartsWith("game:grass-", StringComparison.OrdinalIgnoreCase) ||
                blockCode.StartsWith("game:forestfloor-", StringComparison.OrdinalIgnoreCase) ||
                blockCode.StartsWith("game:peat", StringComparison.OrdinalIgnoreCase) ||
                blockCode.StartsWith("game:farmland-", StringComparison.OrdinalIgnoreCase);
        }
    }
}