using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Cutscene Assets")]
    public Sprite[] arrivalPortraits; // Gambar portrait pesawat

    [TextArea(2, 5)]
    public string[] arrivalDialogues; // Teks cerita/dialogue untuk masing-masing gambar

    [Header("UI Buttons")]
    public Button continueButton;

    void Start()
    {
        Time.timeScale = 1f;
        RefreshContinueButtonState();
    }

    public void RefreshContinueButtonState()
    {
        FreeRoamManager.ApplyButtonUnlockVisual(continueButton, PlayerPrefs.HasKey("SavedScene"));
    }

    public void StartNewGame()
    {
        PlayerPrefs.DeleteAll();
        QuestJournalManager.InvalidatePersistentQuestCache();

        if (QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.ResetAllProgress(false);
        }

        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.ResetAllProgress();
        }

        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.ResetProgress();
        }

        if (MasterQuestManager.Instance != null)
        {
            MasterQuestManager.Instance.ResetAllProgress();
        }

        if (QuestController.Instance != null)
        {
            QuestController.Instance.ActiveQuests.Clear();
        }

        // Reset sistem sampah global
        if (GlobalTrashManager.Instance != null)
        {
            GlobalTrashManager.Instance.ResetAll();
        }

        PlayerPrefs.SetString("SavedScene", "02_CutsceneAirstrip");
        PlayerPrefs.Save();

        if (CutsceneTransitionManager.Instance != null && arrivalPortraits != null && arrivalPortraits.Length > 0)
        {
            CutsceneTransitionManager.Instance.StartPortraitCutscene(arrivalPortraits, arrivalDialogues, "02_CutsceneAirstrip");
        }
        else
        {
            Debug.Log("[MainMenu] Memuat 02_CutsceneAirstrip langsung.");
            SceneManager.LoadScene("02_CutsceneAirstrip");
        }
    }

    public void ContinueGame()
    {
        string sceneToLoad = PlayerPrefs.GetString("SavedScene", "02_Waisai");
        bool shouldRestorePos = true;

        if (FreeRoamManager.IsFreeRoamUnlocked() &&
            (string.IsNullOrEmpty(sceneToLoad) ||
             sceneToLoad == "11_Waisai Ending" ||
             sceneToLoad == "12_CutsceneAirstripEnding" ||
             sceneToLoad == "02_CutsceneAirstrip"))
        {
            sceneToLoad = "02_Waisai";
            shouldRestorePos = false;
            PlayerPrefs.SetString("SavedScene", "02_Waisai");
            PlayerPrefs.DeleteKey("PlayerX");
            PlayerPrefs.DeleteKey("PlayerY");
            PlayerPrefs.DeleteKey("PlayerZ");
        }

        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.LoadProgress();
        }

        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.CalculatePercentage();
        }

        PlayerPrefs.SetInt("ShouldRestoreSavedPosition", shouldRestorePos ? 1 : 0);
        PlayerPrefs.Save();

        Time.timeScale = 1f;
        Debug.Log($"[MainMenu] Melanjutkan game ke scene: {sceneToLoad}");
        SceneManager.LoadScene(sceneToLoad);
    }

    public void QuitGame()
    {
        Debug.Log("Game Quit!");
        Application.Quit();
    }
}
