using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BlankMenuController : MonoBehaviour
{
    [Header("Scene")]
    public string startingScenarioSceneName = "Starting_Scenario";

    [Header("UI References")]
    public GameObject panelMenu;
    public GameObject panelLoading;

    public TMP_InputField nicknameInput;
    public TMP_Text modeLabel;
    public TMP_Text errorText;

    private StudyMode _selectedMode = StudyMode.AI;

    private void Start()
    {
        panelLoading.SetActive(false);
        panelMenu.SetActive(true);
        errorText.text = "";
        SetModeUI(_selectedMode);
    }

    public void OnSelectNoAI() => SetModeUI(StudyMode.NoAI);
    public void OnSelectAI()   => SetModeUI(StudyMode.AI);
    public void OnSelectTest() => SetModeUI(StudyMode.TestAI);

    private void SetModeUI(StudyMode mode)
    {
        _selectedMode = mode;

        if (StudyConditionManager.Instance != null)
            StudyConditionManager.Instance.SetMode(mode);

        if (modeLabel != null)
            modeLabel.text = $"Mode: {mode}";
    }

    public void OnStartPressed()
    {
        errorText.text = "";

        string nick = nicknameInput != null ? nicknameInput.text.Trim() : "";
        bool requiresNick = (_selectedMode != StudyMode.TestAI);

        if (requiresNick && string.IsNullOrEmpty(nick))
        {
            errorText.text = "Enter a nickname (Test mode doesn't require it).";
            return;
        }

        if (StudySessionManager.Instance != null)
        {
            StudySessionManager.Instance.nickname = nick;
            StudySessionManager.Instance.SetMode(_selectedMode);
        }

        StartCoroutine(LoadStartingScenarioAsync());

        StudySessionManager.Instance.BeginRun();
    }

    private IEnumerator LoadStartingScenarioAsync()
    {
        panelMenu.SetActive(false);
        panelLoading.SetActive(true);

        yield return null;
        yield return new WaitForEndOfFrame();

        AsyncOperation op = SceneManager.LoadSceneAsync(startingScenarioSceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
            yield return null;

        yield return null;

        op.allowSceneActivation = true;

        while (!op.isDone)
            yield return null;
    }
}