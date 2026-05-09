using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Botanism
{
    public class BotanismModSystem : ModSystem
    {
        // Temporary startup logs while the mod foundation is being set up.
        public override void Start(ICoreAPI api)
        {
            Mod.Logger.Notification("Botanism loaded");
            Mod.Logger.Notification("Botanism language test: " + Lang.Get("botanism:hello"));
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