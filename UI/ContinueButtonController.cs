namespace QuickContinue;

internal sealed class ContinueButtonController
{
    private const string ContinueButtonName = "QuickContinueButton";
    private const string ContinueButtonLabel = "Continue";
    private const string ContinueUnavailableMessage = "Continue requested, but no valid save target was found.";

    private readonly ContinueService _continueService;
    private readonly ManualLogSource _logger;

    internal ContinueButtonController(ContinueService continueService, ManualLogSource logger)
    {
        _continueService = continueService;
        _logger = logger;
    }

    internal void OnMainMenuEnabled(MainMenu menu)
    {
        if (menu == null || menu.LoadGameButton == null)
        {
            return;
        }

        var continueButton = EnsureContinueButton(menu);
        if (continueButton == null)
        {
            return;
        }

        continueButton.interactable = _continueService.HasAnyContinueCandidate();
    }

    private Button? EnsureContinueButton(MainMenu menu)
    {
        var parent = menu.LoadGameButton.transform.parent;
        if (parent == null)
        {
            return null;
        }

        var existingButton = FindExistingContinueButton(parent);
        if (existingButton != null)
        {
            BindContinueClick(existingButton, menu);
            SetButtonLabel(existingButton, ContinueButtonLabel);
            return existingButton;
        }

        var createdButton = CreateContinueButton(menu, parent);
        if (createdButton == null)
        {
            return null;
        }

        BindContinueClick(createdButton, menu);
        SetButtonLabel(createdButton, ContinueButtonLabel);
        return createdButton;
    }

    private static Button? FindExistingContinueButton(Transform parent)
    {
        var marker = parent.GetComponentsInChildren<ContinueMarker>(includeInactive: true).FirstOrDefault();
        if (marker == null)
        {
            return null;
        }

        return marker.TryGetComponent<Button>(out var existingButton) ? existingButton : null;
    }

    private static Button? CreateContinueButton(MainMenu menu, Transform parent)
    {
        var cloneGo = UnityEngine.Object.Instantiate(menu.LoadGameButton.gameObject, parent);
        cloneGo.name = ContinueButtonName;
        cloneGo.AddComponent<ContinueMarker>();
        cloneGo.transform.SetSiblingIndex(menu.LoadGameButton.transform.GetSiblingIndex());
        return cloneGo.GetComponent<Button>();
    }

    private void BindContinueClick(Button button, MainMenu menu)
    {
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => OnContinuePressed(menu));
    }

    private void OnContinuePressed(MainMenu menu)
    {
        var manager = Singleton<SavingLoadingManager>.Instance;
        if (manager == null || manager.IsCurrentlyLoadingGame)
        {
            return;
        }

        if (!_continueService.TryGetContinueTarget(out var fullPath, out var sceneName, out var saveName, out var gameMode))
        {
            _logger.LogWarning(ContinueUnavailableMessage);
            return;
        }

        _continueService.SetLastSaveName(saveName);
        HideMainMenuPanels(menu);
        manager.LoadSceneThenLoadSave(fullPath, sceneName, gameMode);
    }

    private static void HideMainMenuPanels(MainMenu menu)
    {
        menu.NewGameMenu?.SetActive(false);
        menu.SaveGameMenu?.SetActive(false);
        menu.MainUIPanel?.SetActive(false);
    }

    private static void SetButtonLabel(Button button, string label)
    {
        var tmpTexts = button.GetComponentsInChildren<TMP_Text>(includeInactive: true);
        foreach (var text in tmpTexts)
        {
            text.text = label;
        }

        var legacyTexts = button.GetComponentsInChildren<Text>(includeInactive: true);
        foreach (var text in legacyTexts)
        {
            text.text = label;
        }
    }
}
