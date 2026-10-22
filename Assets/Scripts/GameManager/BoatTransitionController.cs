using UnityEngine;
using UnityEngine.SceneManagement;

public class BoatTransitionController : MonoBehaviour
{
    [Header("Referensi Sistem")]
    public Dialogue dialogueSystem;       
    public TriviaQuizUI triviaSystem;     

    [Header("Data Pulau (Isi di Inspector)")]
    public IslandTransitionData[] islandData;

    private string currentTarget;
    private IslandTransitionData currentData;

    private void Start()
    {
        Time.timeScale = 1f;

    if (triviaSystem == null) triviaSystem = TriviaQuizUI.Instance;
    if (dialogueSystem == null) dialogueSystem = FindFirstObjectByType<Dialogue>();
        if (triviaSystem == null) triviaSystem = TriviaQuizUI.Instance;
        if (dialogueSystem == null) dialogueSystem = FindFirstObjectByType<Dialogue>();

        // 1. Cek dari GamePhaseManager
        if (GamePhaseManager.Instance != null && !string.IsNullOrEmpty(GamePhaseManager.Instance.targetIslandName))
        {
            currentTarget = GamePhaseManager.Instance.targetIslandName;
            Debug.Log($"[BoatTransition] Target dari GamePhaseManager: '{currentTarget}'");
        }
        // 2. Fallback: Cek dari PlayerPrefs (Sangat aman jika GamePhaseManager sempat reset)
        else if (PlayerPrefs.HasKey("TargetIsland"))
        {
            currentTarget = PlayerPrefs.GetString("TargetIsland");
            Debug.Log($"[BoatTransition] Target dari PlayerPrefs: '{currentTarget}'");
        }
        else
        {
            currentTarget = "06_Sawinggrai"; // Fallback default hanya jika testing langsung di scene perahu
            Debug.LogWarning($"[BoatTransition] Target tidak ditemukan. Menggunakan fallback testing: '{currentTarget}'");
        }

        LoadIslandDataAndStart();
    }

    private void LoadIslandDataAndStart()
    {
        if (islandData != null)
        {
            foreach (var data in islandData)
            {
                if (data != null && !string.IsNullOrEmpty(data.islandName))
                {
                    string targetClean = currentTarget.ToLower().Trim();
                    string dataClean = data.islandName.ToLower().Trim();

                    // Pencocokan fleksibel (misal "06_Sawinggrai" cocok dengan "Sawinggrai" atau "06_sawinggrai")
                    if (targetClean == dataClean || targetClean.Contains(dataClean) || dataClean.Contains(targetClean))
                    {
                        currentData = data;
                        Debug.Log($"[BoatTransition] Data dialog & trivia ditemukan untuk: '{data.islandName}'");
                        break;
                    }
                }
            }
        }

        if (currentData != null)
        {
            PlayIntroDialogue();
        }
        else
        {
            Debug.LogError($"[BoatTransition] Data pulau untuk '{currentTarget}' TIDAK DITEMUKAN di Inspector 'Island Data'!", this);
        }
    }

    private void PlayIntroDialogue()
    {
        if (dialogueSystem == null)
        {
            Debug.LogError("[BoatTransition] dialogueSystem belum di-assign di Inspector!", this);
            return;
        }

        if (currentData.npcDialogueLines == null || currentData.npcDialogueLines.Length == 0)
        {
            Debug.LogError($"[BoatTransition] npcDialogueLines untuk '{currentTarget}' kosong di Inspector!", this);
            return;
        }

        dialogueSystem.StartDialogue(currentData.npcDialogueLines, StartTrivia);
    }

    private void StartTrivia()
    {
        if (TriviaQuizUI.Instance != null && currentData != null && currentData.islandTriviaQuestions != null && currentData.islandTriviaQuestions.Length > 0)
        {
            TriviaQuizUI.Instance.StartQuiz(currentData.islandTriviaQuestions, FinishTransition);
        }
        else
        {
            Debug.LogWarning("[BoatTransition] Soal trivia kosong / TriviaQuizUI null, langsung pindah scene.");
            FinishTransition();
        }
    }

    private void FinishTransition()
    {
        PlayerPrefs.SetInt("Visited_" + currentTarget, 1);
        PlayerPrefs.Save();

        // Gunakan sceneName jika diisi, atau fallback ke currentTarget
        string sceneToLoad = !string.IsNullOrEmpty(currentData.targetSceneName) ? currentData.targetSceneName : currentTarget;
        
        Debug.Log($"[BoatTransition] Memuat scene tujuan: '{sceneToLoad}'");
        SceneManager.LoadScene(sceneToLoad);
    }
}

[System.Serializable]
public class IslandTransitionData
{
    public string islandName;       // Misal: "Sawinggrai", "Arborek", "Pianemo"
    public string targetSceneName;  // Nama persis file Scene di Unity (misal: "06_Sawinggrai", "05_Arborek")
    
    [TextArea(2, 5)]
    public string[] npcDialogueLines;
    public TriviaQuestion[] islandTriviaQuestions;
}