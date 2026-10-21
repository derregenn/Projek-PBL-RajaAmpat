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
        if (triviaSystem == null) triviaSystem = TriviaQuizUI.Instance;
        if (dialogueSystem == null) dialogueSystem = FindFirstObjectByType<Dialogue>();

        // 1. Ambil target pulau dari GamePhaseManager
        if (GamePhaseManager.Instance != null && !string.IsNullOrEmpty(GamePhaseManager.Instance.targetIslandName))
        {
            currentTarget = GamePhaseManager.Instance.targetIslandName;
            Debug.Log($"[BoatTransition] Target pulau terdeteksi: '{currentTarget}'");
        }
        else
        {
            // Jika testing langsung di scene kapal, gunakan default Sawinggrai
            currentTarget = "06_Sawinggrai";
            Debug.LogWarning($"[BoatTransition] GamePhaseManager null/kosong. Menggunakan fallback: '{currentTarget}'");
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

                    // Pencocokan fleksibel (misal "06_Sawinggrai" cocok dengan "Sawinggrai")
                    if (targetClean == dataClean || targetClean.Contains(dataClean) || dataClean.Contains(targetClean))
                    {
                        currentData = data;
                        Debug.Log($"[BoatTransition] Menggunakan data dialog untuk: '{data.islandName}'");
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
            Debug.LogError($"[BoatTransition] Data pulau '{currentTarget}' tidak ditemukan di Inspector islandData!", this);
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
            Debug.LogError($"[BoatTransition] npcDialogueLines untuk '{currentTarget}' kosong!", this);
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

        // Pindah ke scene pulau tujuan yang sesungguhnya
        SceneManager.LoadScene(currentTarget);
    }
}

[System.Serializable]
public class IslandTransitionData
{
    public string islandName; 
    [TextArea(2, 5)]
    public string[] npcDialogueLines;
    public TriviaQuestion[] islandTriviaQuestions;
}