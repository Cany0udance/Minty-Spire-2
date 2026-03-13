using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace MintySpire2;

[ModInitializer(nameof(InitializeMod))]
public static class ModInitializer
{
    public static void InitializeMod()
    {
        var harmony = new Harmony("MintySpire2.MintySpire2");

        // Do attribute-style patches too
        harmony.PatchAll();
    }
}