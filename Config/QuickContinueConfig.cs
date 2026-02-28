namespace QuickContinue;

internal sealed class QuickContinueConfig
{
    private const string ConfigFolderName = "QuickContinue";
    private const string ContinueSection = "Continue";
    private const string LastSaveNameKey = "LastSaveName";
    private const string FallbackToMostRecentSaveKey = "FallbackToMostRecentSave";

    private readonly ConfigFile _pluginConfig;

    internal ConfigEntry<string> LastSaveName { get; }
    internal ConfigEntry<bool> FallbackToMostRecentSave { get; }

    internal QuickContinueConfig(ManualLogSource logger)
    {
        var configPath = ResolveConfigPath(logger);
        _pluginConfig = new ConfigFile(configPath, true);

        LastSaveName = _pluginConfig.Bind(
            ContinueSection,
            LastSaveNameKey,
            string.Empty,
            "Last save file used by Continue.");

        FallbackToMostRecentSave = _pluginConfig.Bind(
            ContinueSection,
            FallbackToMostRecentSaveKey,
            true,
            "If LastSaveName is missing/corrupt, use the newest valid save file.");
    }

    private static string ResolveConfigPath(ManualLogSource logger)
    {
        var configDir = Path.Combine(Paths.ConfigPath, ConfigFolderName);
        Directory.CreateDirectory(configDir);

        var newPath = Path.Combine(configDir, $"{ModInfo.Guid}.cfg");
        var legacyPath = Path.Combine(Paths.ConfigPath, $"{ModInfo.Guid}.cfg");

        if (!File.Exists(newPath) && File.Exists(legacyPath))
        {
            TryImportLegacyConfig(logger, legacyPath, newPath);
        }

        return newPath;
    }

    private static void TryImportLegacyConfig(ManualLogSource logger, string legacyPath, string newPath)
    {
        try
        {
            File.Copy(legacyPath, newPath, overwrite: false);
            logger.LogInfo($"Imported existing config to '{newPath}'.");
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Could not import legacy config '{legacyPath}': {ex.Message}");
        }
    }
}
