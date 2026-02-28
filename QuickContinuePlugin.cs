namespace QuickContinue;

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
public sealed class QuickContinuePlugin : BaseUnityPlugin
{
    private Harmony? _harmony;

    private void Awake()
    {
        QuickContinueRuntime.Initialize(Logger);

        _harmony = new Harmony(ModInfo.Guid);
        _harmony.PatchAll();
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
}
