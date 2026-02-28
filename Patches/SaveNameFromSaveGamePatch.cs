namespace QuickContinue;

[HarmonyPatch(typeof(SavingLoadingManager), nameof(SavingLoadingManager.SaveGame), typeof(string), typeof(bool))]
internal static class SaveNameFromSaveGamePatch
{
    private static void Prefix(string saveFileName)
    {
        QuickContinueRuntime.ContinueService?.SetLastSaveName(saveFileName);
    }
}
