namespace QuickContinue;

internal sealed class ContinueService
{
    private const string DefaultGameplaySceneName = "Gameplay";

    private readonly QuickContinueConfig _config;

    internal ContinueService(QuickContinueConfig config)
    {
        _config = config;
    }

    internal void SetLastSaveName(string? saveName)
    {
        var cleaned = Path.GetFileNameWithoutExtension(saveName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return;
        }

        _config.LastSaveName.Value = cleaned;
    }

    internal void SetLastSaveNameFromLoadedGame(SavingLoadingManager manager, string fullFilePath)
    {
        var fromActive = manager.ActiveSaveFileName;
        var candidate = string.IsNullOrWhiteSpace(fromActive)
            ? Path.GetFileNameWithoutExtension(fullFilePath)
            : fromActive;

        SetLastSaveName(candidate);
    }

    internal bool HasAnyContinueCandidate()
    {
        if (HasConfiguredSaveCandidate())
        {
            return true;
        }

        if (!_config.FallbackToMostRecentSave.Value)
        {
            return false;
        }

        return SavingLoadingManager.GetAllSaveFileHeaderFileCombos().Count > 0;
    }

    internal bool TryGetContinueTarget(out string fullPath, out string sceneName, out string saveName)
    {
        if (TryResolveFromConfiguredName(out fullPath, out sceneName, out saveName))
        {
            return true;
        }

        if (!_config.FallbackToMostRecentSave.Value)
        {
            return false;
        }

        return TryResolveMostRecent(out fullPath, out sceneName, out saveName);
    }

    private bool HasConfiguredSaveCandidate()
    {
        var configured = _config.LastSaveName.Value;
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

        var configured = _config.LastSaveName.Value;
        if (string.IsNullOrWhiteSpace(configured))
        {
            return false;
        }

        var candidatePath = SavingLoadingManager.GetFullSaveFilePath(configured);
        var header = SavingLoadingManager.GetSaveFileHeader(candidatePath);
        if (header == null)
        {
            return false;
        }

        if (!TryGetSceneName(header, out sceneName))
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
            .OrderByDescending(item => item.SortUtc);

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
            if (TryParseWithInvariantCulture(header.SaveTimestamp, out var invariantParsed))
            {
                return invariantParsed;
            }

            if (DateTime.TryParse(header.SaveTimestamp, out var parsed))
            {
                return parsed.ToUniversalTime();
            }
        }

        return TryGetLastWriteTimeUtc(fullPath);
    }

    private static bool TryParseWithInvariantCulture(string timestamp, out DateTime parsed)
    {
        return DateTime.TryParse(
            timestamp,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal,
            out parsed);
    }

    private static DateTime TryGetLastWriteTimeUtc(string fullPath)
    {
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
}
