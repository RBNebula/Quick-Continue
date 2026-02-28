namespace QuickContinue;

internal static class QuickContinueRuntime
{
    internal static ManualLogSource? Logger { get; private set; }
    internal static QuickContinueConfig? Config { get; private set; }
    internal static ContinueService? ContinueService { get; private set; }
    internal static ContinueButtonController? ContinueButtonController { get; private set; }

    internal static void Initialize(ManualLogSource logger)
    {
        Logger = logger;
        Config = new QuickContinueConfig(logger);
        ContinueService = new ContinueService(Config);
        ContinueButtonController = new ContinueButtonController(ContinueService, logger);
    }
}
