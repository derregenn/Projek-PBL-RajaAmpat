using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GagFaderManager : MonoBehaviour
{
    private bool isTransitioning = false;
    public float durasiFade = 1.0f;

    [Header("Transisi 1: Peneliti Pamit Pulang (Quest 2 Minta Air)")]
    [Tooltip("Drag NPC_Peneliti_Awal (Peneliti Cewek) ke sini")]
    public GameObject npcPeneliti;

    [Header("Events")]
    [Tooltip("Dipanggil tepat SETELAH fade in selesai (layar kembali terang)")]
    public UnityEvent OnPenelitiPulangFadeCompleted;

    [Header("Transisi 2: Ending Pulau Gag (Pindah Scene)")]
    public string sceneTujuanEnding = "02_Waisai";

    [Header("Cutscene Sunset Pulau Gag")]
    public CutsceneTransitionManager cutsceneManager;
    public Sprite[] sunsetPortraits;
    [TextArea(2, 5)]
    public string[] sunsetDialogues;

    [Header("Event Tamat Game")]
    public UnityEvent OnGameFinished;

    // =========================================================
    // PUBLIC METHODS FOR INSPECTOR EVENTS
    // =========================================================

    // 1. Panggil ini di On Dialogue End NPC_Peneliti_Awal saat MC minta air bersih
    public void PenelitiPulang()
    {
        if (!isTransitioning) StartCoroutine(RoutinePenelitiPulang());
    }

    // 2. Panggil ini di On Dialogue End TourGuide di Dermaga
    public void PindahKeWaisai()
    {
        RecordGagCompletion();

        CutsceneTransitionManager ctm = cutsceneManager != null ? cutsceneManager : CutsceneTransitionManager.Instance;
        if (ctm == null) ctm = FindFirstObjectByType<CutsceneTransitionManager>(FindObjectsInactive.Include);

        if (ctm != null && sunsetPortraits != null && sunsetPortraits.Length > 0)
        {
            ctm.StartPortraitCutscene(sunsetPortraits, sunsetDialogues, sceneTujuanEnding);
        }
        else if (!isTransitioning)
        {
            StartCoroutine(RoutinePindahScene(sceneTujuanEnding));
        }
    }

    // 3. Panggil ini saat cerita Pulau Gag dan seluruh quest tamat
    public void TamatGame()
    {
        RecordGagCompletion();

        CutsceneTransitionManager ctm = cutsceneManager != null ? cutsceneManager : CutsceneTransitionManager.Instance;
        if (ctm == null) ctm = FindFirstObjectByType<CutsceneTransitionManager>(FindObjectsInactive.Include);

        string nextScene = !string.IsNullOrEmpty(sceneTujuanEnding) ? sceneTujuanEnding : "02_Waisai";
        if (ctm != null && sunsetPortraits != null && sunsetPortraits.Length > 0)
        {
            ctm.StartPortraitCutscene(sunsetPortraits, sunsetDialogues, nextScene);
        }
        else if (!isTransitioning)
        {
            StartCoroutine(RoutineTamatGame());
        }
    }

    public void PindahKeScene(string sceneName)
    {
        if (!isTransitioning) StartCoroutine(RoutinePindahScene(sceneName));
    }

    // =========================================================
    // COROUTINES
    // =========================================================

    private IEnumerator RoutinePenelitiPulang()
    {
        isTransitioning = true;
        ScreenFader fader = ScreenFader.Instance != null ? ScreenFader.Instance : FindFirstObjectByType<ScreenFader>();

        // Fade out ke hitam
        if (fader != null) yield return StartCoroutine(fader.FadeOutRoutine());
        else yield return new WaitForSeconds(durasiFade);

        CloseDialogueUI();

        // Nonaktifkan Peneliti Cewek (pamit pulang ke lab pusat)
        if (npcPeneliti != null)
        {
            ResetKamera(npcPeneliti);
            npcPeneliti.SetActive(false);
        }

        yield return new WaitForSeconds(0.3f);

        // Fade in kembali ke game
        if (fader != null) yield return StartCoroutine(fader.FadeInRoutine());
        isTransitioning = false;

        if (QuestController.Instance != null)
        {
            QuestController.Instance.ProgressObjective("serahkan_sampel", 1);
        }

        OnPenelitiPulangFadeCompleted?.Invoke();
    }

    private IEnumerator RoutinePindahScene(string sceneName)
    {
        isTransitioning = true;
        ScreenFader fader = ScreenFader.Instance != null ? ScreenFader.Instance : FindFirstObjectByType<ScreenFader>();

        if (fader != null) yield return StartCoroutine(fader.FadeOutRoutine());
        else yield return new WaitForSeconds(durasiFade);

        CloseDialogueUI();

        // Catat progres pulau selesai
        RecordGagCompletion();

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator RoutineTamatGame()
    {
        isTransitioning = true;
        ScreenFader fader = ScreenFader.Instance != null ? ScreenFader.Instance : FindFirstObjectByType<ScreenFader>();

        if (fader != null) yield return StartCoroutine(fader.FadeOutRoutine());
        else yield return new WaitForSeconds(durasiFade);

        CloseDialogueUI();
        RecordGagCompletion();

        OnGameFinished?.Invoke();

        yield return new WaitForSeconds(1.0f);

        // Pindah ke Main Menu atau scene tujuan ending
        string nextScene = !string.IsNullOrEmpty(sceneTujuanEnding) ? sceneTujuanEnding : "01_MainMenu";
        SceneManager.LoadScene(nextScene);
    }

    private void RecordGagCompletion()
    {
        if (QuestController.Instance != null)
        {
            QuestController.Instance.ProgressObjective("Kembali", 1);
            QuestController.Instance.CompleteAllActiveQuests();
        }

        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.CompleteIsland(IslandName.Gag);
            GamePhaseManager.Instance.SetPercentageDirectly(100f);
        }

        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.CompleteIsland(6); // Indeks 6 = Gag
        }

        PlayerPrefs.SetInt("Visited_10_Gag", 1);
        PlayerPrefs.SetInt("GameCompleted", 1);
        PlayerPrefs.Save();
        Debug.Log("<color=gold>[GagFaderManager] Game Selesai! Selamat Anda Telah Menamatkan Game!</color>");
    }

    private void ResetKamera(GameObject npc)
    {
        if (npc == null) return;
        Dialogue dialogue = npc.GetComponentInChildren<Dialogue>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("Player");

        if (dialogue != null && dialogue.dialogueCamera != null)
        {
            dialogue.dialogueCamera.Priority.Value = 0;
            if (playerObj != null) dialogue.dialogueCamera.Target.TrackingTarget = playerObj.transform;
        }
    }

    private void CloseDialogueUI()
    {
        GameObject dialogueBox = GameObject.Find("DialogueBox");
        if (dialogueBox != null) dialogueBox.SetActive(false);

        GameObject playerDialogueBox = GameObject.Find("PlayerDialogueBox");
        if (playerDialogueBox != null) playerDialogueBox.SetActive(false);
    }
}
