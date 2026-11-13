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
        // Pastikan Player & NPC di-anchor ke kapal dan kontrol player dinonaktifkan
        BoatBobbing bobbing = FindFirstObjectByType<BoatBobbing>();
        if (bobbing != null)
        {
            bobbing.InitializeAnchoring();
        }

        if (triviaSystem == null) triviaSystem = TriviaQuizUI.Instance;
        if (dialogueSystem == null) dialogueSystem = FindFirstObjectByType<Dialogue>();

        // 1. Ambil target pulau dari GamePhaseManager
        if (GamePhaseManager.Instance != null && !string.IsNullOrEmpty(GamePhaseManager.Instance.targetIslandName))
        {
            currentTarget = GamePhaseManager.Instance.targetIslandName;
            Debug.Log($"[BoatTransition] Target pulau terdeteksi: '{currentTarget}'");
        }
        // 2. Fallback: Cek dari PlayerPrefs (Sangat aman jika GamePhaseManager sempat reset)
        else if (PlayerPrefs.HasKey("TargetIsland"))
        {
            currentTarget = PlayerPrefs.GetString("TargetIsland");
            Debug.Log($"[BoatTransition] Target dari PlayerPrefs: '{currentTarget}'");
        }
        else
        {
            // Jika testing langsung di scene kapal, gunakan default Sawinggrai
            currentTarget = "06_Sawinggrai";
            Debug.LogWarning($"[BoatTransition] Target tidak ditemukan. Menggunakan fallback: '{currentTarget}'");
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
            SpawnParallaxBackground(); // <-- Fitur Spawn Background ditambahkan di sini
            PlayIntroDialogue();
        }
        else
        {
            Debug.LogError($"[BoatTransition] Data pulau '{currentTarget}' tidak ditemukan di Inspector islandData!", this);
        }
    }

    // <-- Method baru untuk Spawn Background
    private void SpawnParallaxBackground()
    {
        if (currentData.islandBackgroundPrefab != null && environmentParent != null)
        {
            // Simpan posisi lokal asli dari prefab sebelum di-clone
            Vector3 originalLocalPos = currentData.islandBackgroundPrefab.transform.localPosition;

            // Spawn background dan simpan ke variabel
            GameObject spawnedBG = Instantiate(currentData.islandBackgroundPrefab, environmentParent);
            
            // Kembalikan posisi lokal ke posisi asli prefab agar air laut tetap sejajar dengan perahu
            spawnedBG.transform.localPosition = originalLocalPos;
            
            // Jaga-jaga jika prefab tersimpan dalam keadaan mati, kita paksa hidupkan
            spawnedBG.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[BoatTransition] Prefab Background atau Environment Parent belum di-assign di Inspector.");
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
        int buildIndex = FindTargetSceneBuildIndex();
        if (buildIndex < 0)
        {
            Debug.LogError($"[BoatTransition] Scene '{currentTarget}' tidak ditemukan di Build Settings. Tambahkan scene tersebut ke Scenes In Build dan pastikan namanya cocok.", this);
            return;
        }

        PlayerPrefs.SetInt("Visited_" + currentTarget, 1);
        PlayerPrefs.Save();

        BoatBobbing bobbing = FindFirstObjectByType<BoatBobbing>();
        if (bobbing != null)
        {
            bobbing.ReleasePassengers();
        }

        // Pindah ke scene pulau tujuan yang sesungguhnya
        SceneManager.LoadScene(buildIndex);
    }

    private int FindTargetSceneBuildIndex()
    {
        string targetName = currentTarget.Trim();
        int separatorIndex = Mathf.Max(targetName.LastIndexOf('/'), targetName.LastIndexOf('\\'));
        if (separatorIndex >= 0)
            targetName = targetName.Substring(separatorIndex + 1);
        if (targetName.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
            targetName = targetName.Substring(0, targetName.Length - 6);

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            int pathSeparatorIndex = Mathf.Max(scenePath.LastIndexOf('/'), scenePath.LastIndexOf('\\'));
            string sceneName = scenePath.Substring(pathSeparatorIndex + 1);
            if (sceneName.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
                sceneName = sceneName.Substring(0, sceneName.Length - 6);

            if (string.Equals(sceneName, targetName, System.StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }
}

[System.Serializable]
public class IslandTransitionData
{
    public string islandName;
    
    [Header("Visual Environment (Parallax)")]
    [Tooltip("Masukkan Prefab Parallax Background (contoh: BG_Arborek) dari panel Project")]
    public GameObject islandBackgroundPrefab; // <-- Slot tambahan untuk prefab background

    [TextArea(2, 5)]
    public string[] npcDialogueLines;
    public TriviaQuestion[] islandTriviaQuestions;
}