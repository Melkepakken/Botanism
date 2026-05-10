using System;
using Botanism.Profiles;
using Botanism.Systems;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Botanism.BlockBehaviors
{
    public class BlockBehaviorPropaguleExtraction : BlockBehavior
    {
        private static readonly AssetLocation ExtractionSound =
            new AssetLocation("game", "sounds/block/leafy-picking");

        private const string ExtractionAnimation = "knifecut";
        private const float FallbackExtractionSeconds = 1.5f;
        private const float MinimumExtractionSeconds = 0.1f;

        private PlantExtractionService extractionService;

        public BlockBehaviorPropaguleExtraction(Block block) : base(block)
        {
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            extractionService = api.ModLoader.GetModSystem<PlantExtractionService>();
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

            if (!CanStartExtraction(world, byPlayer, blockSel, out _))
            {
                return false;
            }

            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
            {
                return false;
            }

            handling = EnumHandling.PreventDefault;

            world.PlaySoundAt(
                ExtractionSound,
                blockSel.Position,
                0,
                byPlayer
            );

            StartExtractionAnimation(world, byPlayer);

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

            if (!CanStartExtraction(world, byPlayer, blockSel, out PlantProfile profile))
            {
                StopExtractionAnimation(byPlayer);
                return false;
            }

            handling = EnumHandling.PreventDefault;

            float extractionSeconds = GetExtractionSeconds(profile);

            StartExtractionAnimation(world, byPlayer);
            TryPlayHarvestEffects(world, byPlayer, blockSel);

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

            if (!CanStartExtraction(world, byPlayer, blockSel, out PlantProfile profile))
            {
                StopExtractionAnimation(byPlayer);
                return;
            }

            handling = EnumHandling.PreventDefault;

            float extractionSeconds = GetExtractionSeconds(profile);

            StopExtractionAnimation(byPlayer);

            if (world.Side != EnumAppSide.Server)
            {
                return;
            }

            if (secondsUsed < extractionSeconds - 0.05f)
            {
                return;
            }

            extractionService.TryExtractPlant(
                world,
                byPlayer,
                blockSel,
                damageTool: true
            );
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(
            IWorldAccessor world,
            BlockSelection selection,
            IPlayer forPlayer,
            ref EnumHandling handling
        )
        {
            if (extractionService == null)
            {
                return Array.Empty<WorldInteraction>();
            }

            PlantProfile profile = extractionService.GetProfileForSelection(world, selection);

            if (profile == null)
            {
                return Array.Empty<WorldInteraction>();
            }

            ItemStack[] toolStacks = extractionService.GetToolStacksForProfile(world, profile);

            if (toolStacks.Length == 0)
            {
                return Array.Empty<WorldInteraction>();
            }

            return new[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "botanism:blockhelp-extract",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = toolStacks
                }
            };
        }

        public override int GetPlacedBlockInteractionHelpCount(
            IWorldAccessor world,
            BlockSelection selection,
            IPlayer forPlayer,
            ref EnumHandling handling
        )
        {
            if (extractionService == null)
            {
                return 0;
            }

            PlantProfile profile = extractionService.GetProfileForSelection(world, selection);

            if (profile == null)
            {
                return 0;
            }

            ItemStack[] toolStacks = extractionService.GetToolStacksForProfile(world, profile);

            return toolStacks.Length > 0
                ? 1
                : 0;
        }

        private bool CanStartExtraction(
            IWorldAccessor world,
            IPlayer byPlayer,
            BlockSelection blockSel,
            out PlantProfile profile
        )
        {
            profile = null;

            if (world == null || byPlayer == null || blockSel == null)
            {
                return false;
            }

            if (extractionService == null)
            {
                return false;
            }

            return extractionService.CanExtractPlant(
                world,
                byPlayer,
                blockSel,
                out profile
            );
        }

        private static float GetExtractionSeconds(PlantProfile profile)
        {
            if (profile == null)
            {
                return FallbackExtractionSeconds;
            }

            return Math.Max(MinimumExtractionSeconds, profile.ExtractionSeconds);
        }

        private static void StartExtractionAnimation(IWorldAccessor world, IPlayer byPlayer)
        {
            EntityAgent entity = byPlayer?.Entity;

            if (entity == null)
            {
                return;
            }

            if (world.Side == EnumAppSide.Client)
            {
                entity.StartAnimation(ExtractionAnimation);
                (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
            }
        }

        private static void StopExtractionAnimation(IPlayer byPlayer)
        {
            byPlayer?.Entity?.StopAnimation(ExtractionAnimation);
        }

        private static void TryPlayHarvestEffects(
            IWorldAccessor world,
            IPlayer byPlayer,
            BlockSelection blockSel
        )
        {
            if (world == null || byPlayer == null || blockSel == null)
            {
                return;
            }

            if (world.Rand.NextDouble() < 0.05)
            {
                world.PlaySoundAt(
                    ExtractionSound,
                    blockSel.Position,
                    0,
                    byPlayer
                );
            }

            if (world.Side == EnumAppSide.Client)
            {
                (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
            }
        }
    }
}