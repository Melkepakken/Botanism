using Botanism.BlockBehaviors;
using Botanism.BlockEntities;
using Botanism.Items;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Botanism
{
    public class BotanismModSystem : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterItemClass("Propagule", typeof(ItemPropagule));
            api.RegisterBlockEntityClass("PlantedPropagule", typeof(BlockEntityPlantedPropagule));
            api.RegisterBlockBehaviorClass("PropaguleExtraction", typeof(BlockBehaviorPropaguleExtraction));

            Mod.Logger.Notification("Botanism loaded");
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Botanism server systems initialized");
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Botanism client systems initialized");
        }
    }
}