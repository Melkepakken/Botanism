using Botanism.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Botanism.Systems
{
    public class PlantExtractionSystem : ModSystem
    {
        private ICoreServerAPI sapi;
        private PlantExtractionService extractionService;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            extractionService = api.ModLoader.GetModSystem<PlantExtractionService>();

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
            if (byPlayer == null || blockSel == null)
            {
                return;
            }

            if (extractionService == null)
            {
                return;
            }

            if (!extractionService.TryExtractPlant(sapi.World, byPlayer, blockSel))
            {
                return;
            }

            handling = EnumHandling.PreventDefault;
        }
    }
}