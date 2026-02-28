namespace QuickContinue;

[HarmonyPatch(typeof(MainMenu), "OnEnable")]
internal static class MainMenuOnEnablePatch
{
    private static void Postfix(MainMenu __instance)
    {
        QuickContinueRuntime.ContinueButtonController?.OnMainMenuEnabled(__instance);
    }
}
