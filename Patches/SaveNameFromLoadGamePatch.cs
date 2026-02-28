namespace QuickContinue;

[HarmonyPatch(typeof(SavingLoadingManager), nameof(SavingLoadingManager.LoadGame))]
internal static class SaveNameFromLoadGamePatch
{
    private static void Postfix(SavingLoadingManager __instance, string fullFilePath)
    {
        QuickContinueRuntime.ContinueService?.SetLastSaveNameFromLoadedGame(__instance, fullFilePath);
    }
}
