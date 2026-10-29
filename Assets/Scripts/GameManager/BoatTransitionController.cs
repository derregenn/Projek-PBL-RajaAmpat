using UnityEngine;
using UnityEngine.SceneManagement;

public class BoatTransitionController : MonoBehaviour
{
    [Header("Referensi Sistem")]
    public Dialogue dialogueSystem;       
    public TriviaQuizUI triviaSystem;     

    [Header("Lokasi Spawn Background")]
    [Tooltip("Pilih objek Environment sebagai tempat munculnya background parallax")]
    public Transform environmentParent; // Drag objek 'Environment' dari Hierarchy ke sini

    [Header("Data Pulau (Isi di Inspector)")]
    public IslandTransitionData[] islandData;

    private string currentTarget;
    private IslandTransitionData currentData;

    private void Start()
    {
        Time.timeScale = 1f;

        if (triviaSystem == null) triviaSystem = TriviaQuizUI.Instance;
        if (dialogueSystem == null) dialogueSystem = FindFirstObjectByType<Dialogue>();

        if (GamePhaseManager.Instance != null && !string.IsNullOrEmpty(GamePhaseManager.Instance.targetIslandName))
        {
            currentTarget = GamePhaseManager.Instance.targetIslandName;
        }
        else if (PlayerPrefs.HasKey("TargetIsland"))
        {
            currentTarget = PlayerPrefs.GetString("TargetIsland");
        }
        else
        {
            currentTarget = "06_Sawinggrai"; 
            Debug.LogWarning($"Target tidak ditemukan. Fallback: '{currentTarget}'");
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

                    if (targetClean == dataClean || targetClean.Contains(dataClean) || dataClean.Contains(targetClean))
                    {
                        currentData = data;
                        break;
                    }
                }
            }
        }

        if (currentData != null)
        {
            SpawnParallaxBackground();
            PlayIntroDialogue();
        }
        else
        {
            Debug.LogError($"Data pulau '{currentTarget}' TIDAK DITEMUKAN di Inspector 'Island Data'!", this);
        }
    }

private void SpawnParallaxBackground()
    {
        if (currentData.islandBackgroundPrefab != null && environmentParent != null)
        {
            // Spawn background dan simpan ke variabel
            GameObject spawnedBG = Instantiate(currentData.islandBackgroundPrefab, environmentParent);
            
            // PAKSA posisi lokalnya ke titik tengah (0,0,0) agar pasti masuk kamera
            spawnedBG.transform.localPosition = Vector3.zero;
            
            // Jaga-jaga jika prefab tersimpan dalam keadaan mati, kita paksa hidupkan
            spawnedBG.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Prefab Background atau Environment Parent belum di-assign di Inspector.");
        }
    }
    private void PlayIntroDialogue()
    {
        if (dialogueSystem == null) return;
        if (currentData.npcDialogueLines == null || currentData.npcDialogueLines.Length == 0) return;

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
            FinishTransition();
        }
    }

    private void FinishTransition()
    {
        PlayerPrefs.SetInt("Visited_" + currentTarget, 1);
        PlayerPrefs.Save();

        string sceneToLoad = !string.IsNullOrEmpty(currentData.targetSceneName) ? currentData.targetSceneName : currentTarget;
        SceneManager.LoadScene(sceneToLoad);
    }
}

[System.Serializable]
public class IslandTransitionData
{
    public string islandName;       
    public string targetSceneName;  
    
    [Header("Visual Environment (Parallax)")]
    [Tooltip("Masukkan Prefab Parallax Background (contoh: BG_Arborek) dari panel Project")]
    public GameObject islandBackgroundPrefab; 

    [TextArea(2, 5)]
    public string[] npcDialogueLines;
    public TriviaQuestion[] islandTriviaQuestions;
}