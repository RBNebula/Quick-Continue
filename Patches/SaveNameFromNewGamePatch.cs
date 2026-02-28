namespace QuickContinue;

[HarmonyPatch(typeof(SavingLoadingManager), nameof(SavingLoadingManager.LoadSceneAndStartNewSaveFile))]
internal static class SaveNameFromNewGamePatch
{
    private static void Prefix(string newSaveFileName)
    {
        QuickContinueRuntime.ContinueService?.SetLastSaveName(newSaveFileName);
    }
}
