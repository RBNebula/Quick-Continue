namespace QuickContinue;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class QuickContinuePlugin : BaseUnityPlugin
{
    private const string PluginGuid = "com.rbplex.quickcontinue";
    private const string PluginName = "Quick Continue";
    private const string PluginVersion = "1.0.0";
    private const string ContinueButtonName = "QuickContinueButton";
    private const string DefaultGameplaySceneName = "Gameplay";
    private const string ConfigFolderName = "QuickContinue";

    private static QuickContinuePlugin? _instance;
    private Harmony? _harmony;
    private ConfigFile? _pluginConfig;

    private ConfigEntry<string>? _lastSaveName;
    private ConfigEntry<bool>? _fallbackToMostRecentSave;

    private void Awake()
    {
        _instance = this;
        _pluginConfig = new ConfigFile(ResolveConfigPath(), true);

        _lastSaveName = _pluginConfig.Bind(
            "Continue",
            "LastSaveName",
            string.Empty,
            "Last save file used by Continue.");
        _fallbackToMostRecentSave = _pluginConfig.Bind(
            "Continue",
            "FallbackToMostRecentSave",
            true,
            "If LastSaveName is missing/corrupt, use the newest valid save file.");

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();
        Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private string ResolveConfigPath()
    {
        var configDir = Path.Combine(Paths.ConfigPath, ConfigFolderName);
        Directory.CreateDirectory(configDir);

        var newPath = Path.Combine(configDir, $"{PluginGuid}.cfg");
        var legacyPath = Path.Combine(Paths.ConfigPath, $"{PluginGuid}.cfg");

        if (!File.Exists(newPath) && File.Exists(legacyPath))
        {
            try
            {
                File.Copy(legacyPath, newPath, overwrite: false);
                Logger.LogInfo($"Imported existing config to '{newPath}'.");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not import legacy config '{legacyPath}': {ex.Message}");
            }
        }

        return newPath;
    }

    private void OnDestroy()
    {
        try
        {
            _harmony?.UnpatchSelf();
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Failed to unpatch cleanly: {ex.Message}");
        }
    }

    private void SetLastSaveName(string? saveName)
    {
        if (_lastSaveName == null) return;
        var cleaned = Path.GetFileNameWithoutExtension(saveName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(cleaned)) return;
        _lastSaveName.Value = cleaned;
    }

    private bool TryGetContinueTarget(out string fullPath, out string sceneName, out string saveName)
    {
        fullPath = string.Empty;
        sceneName = string.Empty;
        saveName = string.Empty;

        if (TryResolveFromConfiguredName(out fullPath, out sceneName, out saveName))
        {
            return true;
        }

        if (!(_fallbackToMostRecentSave?.Value ?? true))
        {
            return false;
        }

        return TryResolveMostRecent(out fullPath, out sceneName, out saveName);
    }

    private bool HasAnyContinueCandidate()
    {
        if (HasConfiguredSaveCandidate())
        {
            return true;
        }

        if (!(_fallbackToMostRecentSave?.Value ?? true))
        {
            return false;
        }

        return SavingLoadingManager.GetAllSaveFileHeaderFileCombos().Count > 0;
    }

    private bool HasConfiguredSaveCandidate()
    {
        var configured = _lastSaveName?.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(configured))
        {
            return false;
        }

        var candidatePath = SavingLoadingManager.GetFullSaveFilePath(configured);
        return SavingLoadingManager.GetSaveFileHeader(candidatePath) != null;
    }

    private bool TryResolveFromConfiguredName(out string fullPath, out string sceneName, out string saveName)
    {
        fullPath = string.Empty;
        sceneName = string.Empty;
        saveName = string.Empty;

        var configured = _lastSaveName?.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(configured))
        {
            return false;
        }

        var candidatePath = SavingLoadingManager.GetFullSaveFilePath(configured);
        var header = SavingLoadingManager.GetSaveFileHeader(candidatePath);
        if (header == null || !TryGetSceneName(header, out sceneName))
        {
            return false;
        }

        fullPath = candidatePath;
        saveName = Path.GetFileNameWithoutExtension(candidatePath);
        return true;
    }

    private static bool TryResolveMostRecent(out string fullPath, out string sceneName, out string saveName)
    {
        fullPath = string.Empty;
        sceneName = string.Empty;
        saveName = string.Empty;

        var sorted = SavingLoadingManager.GetAllSaveFileHeaderFileCombos()
            .Select(combo => new
            {
                Combo = combo,
                SortUtc = GetBestUtcSortDate(combo.FullFilePath, combo.SaveFileHeader)
            })
            .OrderByDescending(x => x.SortUtc);

        foreach (var item in sorted)
        {
            if (item.Combo?.SaveFileHeader == null)
            {
                continue;
            }

            if (!TryGetSceneName(item.Combo.SaveFileHeader, out sceneName))
            {
                continue;
            }

            fullPath = item.Combo.FullFilePath;
            saveName = Path.GetFileNameWithoutExtension(fullPath);
            return !string.IsNullOrWhiteSpace(fullPath);
        }

        return false;
    }

    private static DateTime GetBestUtcSortDate(string fullPath, SaveFileHeader? header)
    {
        if (header != null && !string.IsNullOrWhiteSpace(header.SaveTimestamp))
        {
            if (DateTime.TryParse(
                    header.SaveTimestamp,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal,
                    out var parsed))
            {
                return parsed;
            }

            if (DateTime.TryParse(header.SaveTimestamp, out parsed))
            {
                return parsed.ToUniversalTime();
            }
        }

        try
        {
            return File.GetLastWriteTimeUtc(fullPath);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    private static bool TryGetSceneName(SaveFileHeader header, out string sceneName)
    {
        sceneName = string.Empty;
        var levelId = header.LevelID;
        if (!string.IsNullOrWhiteSpace(levelId))
        {
            var levelInfo = Singleton<LevelManager>.Instance?.GetLevelByID(levelId);
            if (levelInfo != null && !string.IsNullOrWhiteSpace(levelInfo.SceneName))
            {
                sceneName = levelInfo.SceneName;
                return true;
            }
        }

        // Fallback for cases where LevelManager metadata is unavailable in main menu.
        sceneName = DefaultGameplaySceneName;
        return true;
    }

    private sealed class ContinueMarker : MonoBehaviour
    {
    }

    [HarmonyPatch(typeof(MainMenu), "OnEnable")]
    private static class MainMenuOnEnablePatch
    {
        private static void Postfix(MainMenu __instance)
        {
            if (_instance == null) return;
            if (__instance == null || __instance.LoadGameButton == null) return;

            var continueButton = EnsureContinueButton(__instance);
            if (continueButton == null) return;

            var canContinue = _instance.HasAnyContinueCandidate();
            continueButton.interactable = canContinue;
        }

        private static Button? EnsureContinueButton(MainMenu menu)
        {
            var parent = menu.LoadGameButton.transform.parent;
            if (parent == null) return null;

            var existing = parent.GetComponentsInChildren<ContinueMarker>(includeInactive: true)
                .FirstOrDefault();

            if (existing != null && existing.TryGetComponent<Button>(out var existingButton))
            {
                BindContinueClick(existingButton, menu);
                SetButtonLabel(existingButton, "Continue");
                return existingButton;
            }

            var cloneGo = Instantiate(menu.LoadGameButton.gameObject, parent);
            cloneGo.name = ContinueButtonName;
            cloneGo.AddComponent<ContinueMarker>();
            cloneGo.transform.SetSiblingIndex(menu.LoadGameButton.transform.GetSiblingIndex());

            var cloneButton = cloneGo.GetComponent<Button>();
            if (cloneButton == null)
            {
                return null;
            }

            BindContinueClick(cloneButton, menu);
            SetButtonLabel(cloneButton, "Continue");
            return cloneButton;
        }

        private static void BindContinueClick(Button button, MainMenu menu)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => OnContinuePressed(menu));
        }

        private static void OnContinuePressed(MainMenu menu)
        {
            if (_instance == null) return;

            var manager = Singleton<SavingLoadingManager>.Instance;
            if (manager == null || manager.IsCurrentlyLoadingGame)
            {
                return;
            }

            if (!_instance.TryGetContinueTarget(out var fullPath, out var sceneName, out var saveName))
            {
                _instance.Logger.LogWarning("Continue requested, but no valid save target was found.");
                return;
            }

            _instance.SetLastSaveName(saveName);

            menu.NewGameMenu?.SetActive(false);
            menu.SaveGameMenu?.SetActive(false);
            menu.MainUIPanel?.SetActive(false);

            manager.LoadSceneThenLoadSave(fullPath, sceneName);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            var text = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (text != null)
            {
                text.text = label;
            }
        }
    }

    [HarmonyPatch(typeof(SavingLoadingManager), nameof(SavingLoadingManager.LoadSceneAndStartNewSaveFile))]
    private static class SaveNameFromNewGamePatch
    {
        private static void Prefix(string newSaveFileName)
        {
            _instance?.SetLastSaveName(newSaveFileName);
        }
    }

    [HarmonyPatch(typeof(SavingLoadingManager), nameof(SavingLoadingManager.LoadGame))]
    private static class SaveNameFromLoadGamePatch
    {
        private static void Postfix(SavingLoadingManager __instance, string fullFilePath)
        {
            var fromActive = __instance.ActiveSaveFileName;
            var candidate = string.IsNullOrWhiteSpace(fromActive)
                ? Path.GetFileNameWithoutExtension(fullFilePath)
                : fromActive;

            _instance?.SetLastSaveName(candidate);
        }
    }

    [HarmonyPatch(typeof(SavingLoadingManager), nameof(SavingLoadingManager.SaveGame), typeof(string), typeof(bool))]
    private static class SaveNameFromSaveGamePatch
    {
        private static void Prefix(string saveFileName)
        {
            _instance?.SetLastSaveName(saveFileName);
        }
    }
}
