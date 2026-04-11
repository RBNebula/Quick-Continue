namespace QuickContinue;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
public sealed class QuickContinuePlugin : BaseUnityPlugin
{
    private Harmony? _harmony;

    private void Awake()
    {
        QuickContinueRuntime.Initialize(Logger);

        _harmony = new Harmony(ModInfo.HARMONY_ID);
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
