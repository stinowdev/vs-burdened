using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Burdened;

/// <summary>
/// Entry point for the mod. 
/// </summary>
public class BurdenedModSystem : ModSystem
{
    public const string ModId = "burdened";

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        api.Logger.Notification("[{0}] client side loaded.", ModId);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        api.Logger.Notification("[{0}] server side loaded.", ModId);
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}
