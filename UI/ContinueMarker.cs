namespace QuickContinue;

internal sealed class ContinueMarker : MonoBehaviour
{
    private const string ContinueButtonLabel = "Continue";

    private void Awake()
    {
        ApplyLabel();
    }

    private void OnEnable()
    {
        ApplyLabel();
    }

    private void LateUpdate()
    {
        ApplyLabel();
    }

    private void ApplyLabel()
    {
        var tmpTexts = GetComponentsInChildren<TMP_Text>(includeInactive: true);
        foreach (var text in tmpTexts)
        {
            if (text.text != ContinueButtonLabel)
            {
                text.text = ContinueButtonLabel;
            }
        }

        var legacyTexts = GetComponentsInChildren<Text>(includeInactive: true);
        foreach (var text in legacyTexts)
        {
            if (text.text != ContinueButtonLabel)
            {
                text.text = ContinueButtonLabel;
            }
        }
    }
}
