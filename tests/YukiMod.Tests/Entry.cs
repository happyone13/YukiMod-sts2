using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using TestTheSpire;

namespace YukiMod.Tests;

[ModInitializer(nameof(Init))]
public static class Entry
{
    public static void Init()
    {
        CombatTestBootstrap.Initialize(Assembly.GetExecutingAssembly(), new CombatTestOptions
        {
            LogPrefix = "YukiMod.Tests"
        });

        Log.Info("[YukiMod.Tests] Mod initialized");
    }
}
