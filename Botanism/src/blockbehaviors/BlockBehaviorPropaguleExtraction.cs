using System;
using Botanism.Profiles;
using Botanism.Systems;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Botanism.BlockBehaviors
{
    public class BlockBehaviorPropaguleExtraction : BlockBehavior
    {
        private static readonly AssetLocation ExtractionSound =
            new AssetLocation("game", "sounds/block/leafy-picking");

        private PlantExtractionService extractionService;

        public BlockBehaviorPropaguleExtraction(Block block) : base(block)
        {
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            extractionService = api.ModLoader.GetModSystem<PlantExtractionService>();
        }

        public override int GetPlacedBlockInteractionHelpCount(
            IWorldAccessor world,
            BlockSelection selection,
            IPlayer forPlayer,
            ref EnumHandling handling
        )
        {
            PlantProfile profile = extractionService?.GetProfileForSelection(world, selection);

            if (profile == null)
            {
                return 0;
            }

            ItemStack[] toolStacks = extractionService.GetToolStacksForProfile(world, profile);

            return toolStacks.Length > 0
                ? 1
                : 0;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(
            IWorldAccessor world,
            BlockSelection selection,
            IPlayer forPlayer,
            ref EnumHandling handling
        )
        {
            PlantProfile profile = extractionService?.GetProfileForSelection(world, selection);

            if (profile == null)
            {
                return Array.Empty<WorldInteraction>();
            }

            ItemStack[] toolStacks = extractionService.GetToolStacksForProfile(world, profile);

            if (toolStacks.Length == 0)
            {
                return Array.Empty<WorldInteraction>();
            }

            handling = EnumHandling.PreventDefault;

            return new[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "botanism:blockhelp-extract",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift",
                    Itemstacks = toolStacks
                }
            };
        }

        public override bool OnBlockInteractStart(
            IWorldAccessor world,
            IPlayer byPlayer,
            BlockSelection blockSel,
            ref EnumHandling handling
        )
        {
            if (extractionService == null)
            {
                return false;
            }

            if (!IsHoldingShift(byPlayer))
            {
                return false;
            }

            if (!extractionService.CanExtractPlant(world, byPlayer, blockSel, out PlantProfile profile))
            {
                return false;
            }

            handling = EnumHandling.PreventDefault;

            PlayExtractionSound(world, byPlayer, blockSel);

            return true;
        }

        public override bool OnBlockInteractStep(
            float secondsUsed,
            IWorldAccessor world,
            IPlayer byPlayer,
            BlockSelection blockSel,
            ref EnumHandling handling
        )
        {
            if (extractionService == null)
            {
                return false;
            }

            if (!IsHoldingShift(byPlayer))
            {
                return false;
            }

            if (!extractionService.CanExtractPlant(world, byPlayer, blockSel, out PlantProfile profile))
            {
                return false;
            }

            if (blockSel == null)
            {
                return false;
            }

            handling = EnumHandling.PreventDefault;

            if (world.Rand.NextDouble() < 0.05)
            {
                PlayExtractionSound(world, byPlayer, blockSel);
            }

            float extractionSeconds = Math.Max(0.1f, profile.ExtractionSeconds);

            return world.Side == EnumAppSide.Client || secondsUsed < extractionSeconds;
        }

        public override void OnBlockInteractStop(
            float secondsUsed,
            IWorldAccessor world,
            IPlayer byPlayer,
            BlockSelection blockSel,
            ref EnumHandling handling
        )
        {
            if (extractionService == null)
            {
                return;
            }

            if (!IsHoldingShift(byPlayer))
            {
                return;
            }

            if (!extractionService.CanExtractPlant(world, byPlayer, blockSel, out PlantProfile profile))
            {
                return;
            }

            handling = EnumHandling.PreventDefault;

            float extractionSeconds = Math.Max(0.1f, profile.ExtractionSeconds);

            if (secondsUsed < extractionSeconds - 0.05f)
            {
                return;
            }

            if (extractionService.TryExtractPlant(world, byPlayer, blockSel))
            {
                PlayExtractionSound(world, byPlayer, blockSel);
            }
        }

        public override bool OnBlockInteractCancel(
            float secondsUsed,
            IWorldAccessor world,
            IPlayer byPlayer,
            BlockSelection blockSel,
            ref EnumHandling handling
        )
        {
            if (extractionService == null)
            {
                return true;
            }

            if (!IsHoldingShift(byPlayer))
            {
                return true;
            }

            if (extractionService.CanExtractPlant(world, byPlayer, blockSel, out PlantProfile profile))
            {
                handling = EnumHandling.PreventDefault;
            }

            return true;
        }

        private static bool IsHoldingShift(IPlayer player)
        {
            return player?.Entity?.Controls?.ShiftKey == true;
        }

        private static void PlayExtractionSound(
            IWorldAccessor world,
            IPlayer player,
            BlockSelection blockSel
        )
        {
            if (blockSel == null)
            {
                return;
            }

            world.PlaySoundAt(
                ExtractionSound,
                blockSel.Position,
                0,
                player
            );
        }
    }
}