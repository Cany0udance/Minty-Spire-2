using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace MintySpire2.util;

public class HookSubscriber
{
    private static readonly MintyHooker CustomModel = ModelDb.Get<MintyHooker>();
    
    public static void subscribe()
    {
        ModHelper.SubscribeForRunStateHooks(MainFile.ModId, rState => [CustomModel] );
        ModHelper.SubscribeForCombatStateHooks(MainFile.ModId, cState => [CustomModel] );
    }
}