using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Botanism.Systems
{
    public class PlantExtractionSystem : ModSystem
    {
        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            // Left-click block-break extraction has been intentionally disabled.
            //
            // Botanism extraction now happens through BlockBehaviorPropaguleExtraction,
            // which exposes supported plants as IHarvestable to vanilla ItemKnife.
            //
            // Do not subscribe to api.Event.BreakBlock here, or players can bypass
            // the intended timed Shift + right-click extraction flow.
            Mod.Logger.Notification("Botanism break-block extraction disabled");
        }
    }
}