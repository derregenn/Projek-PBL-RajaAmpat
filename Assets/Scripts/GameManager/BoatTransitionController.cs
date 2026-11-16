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

    public static string ExtractBaseIslandName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        string clean = raw.ToLower().Trim();
        if (clean.StartsWith("0") || clean.StartsWith("1"))
        {
            int underscore = clean.IndexOf('_');
            if (underscore >= 0 && underscore < clean.Length - 1)
                clean = clean.Substring(underscore + 1);
        }
        return clean;
    }

    /// <summary>
    /// Mengecek apakah cutscene boat trivia untuk pulau tujuan sudah pernah dimainkan atau pulaunya sudah selesai.
    /// </summary>
    public static bool HasCompletedBoatTriviaOrVisited(string targetSceneOrIsland)
    {
        if (string.IsNullOrWhiteSpace(targetSceneOrIsland)) return false;

        if (PlayerPrefs.GetInt("GameCompleted", 0) == 1)
        {
            return true;
        }

        FindTargetSceneBuildIndex(targetSceneOrIsland, out string resolvedSceneName);

        if (PlayerPrefs.GetInt("Visited_" + targetSceneOrIsland, 0) == 1 ||
            (!string.IsNullOrEmpty(resolvedSceneName) && PlayerPrefs.GetInt("Visited_" + resolvedSceneName, 0) == 1))
        {
            return true;
        }

        IslandName? island = QuestJournalManager.GetIslandFromSceneName(
            !string.IsNullOrEmpty(resolvedSceneName) ? resolvedSceneName : targetSceneOrIsland);

        if (!island.HasValue)
        {
            island = QuestJournalManager.GetIslandFromSceneName(targetSceneOrIsland);
        }

        if (island.HasValue)
        {
            if (island.Value == IslandName.Airstrip || island.Value == IslandName.Waisai)
            {
                return true;
            }

            if (PlayerPrefs.GetInt("BoatTriviaDone_" + island.Value.ToString(), 0) == 1)
            {
                return true;
            }

            if (QuestJournalManager.IsIslandCompletedPersistent(island.Value))
            {
                return true;
            }
        }

        string checkName = !string.IsNullOrEmpty(resolvedSceneName) ? resolvedSceneName : targetSceneOrIsland;
        if (checkName.IndexOf("MainMenu", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            checkName.IndexOf("Laut", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Menandai bahwa event boat trivia untuk pulau/scene tujuan telah selesai dijalankan (sekali seumur save).
    /// </summary>
    public static void MarkBoatTriviaCompleted(string targetSceneOrIsland, string resolvedSceneName = null)
    {
        if (!string.IsNullOrWhiteSpace(targetSceneOrIsland))
        {
            PlayerPrefs.SetInt("Visited_" + targetSceneOrIsland, 1);
        }

        if (!string.IsNullOrWhiteSpace(resolvedSceneName))
        {
            PlayerPrefs.SetInt("Visited_" + resolvedSceneName, 1);
        }

        IslandName? island = QuestJournalManager.GetIslandFromSceneName(
            !string.IsNullOrWhiteSpace(resolvedSceneName) ? resolvedSceneName : targetSceneOrIsland);

        if (!island.HasValue && !string.IsNullOrWhiteSpace(targetSceneOrIsland))
        {
            island = QuestJournalManager.GetIslandFromSceneName(targetSceneOrIsland);
        }

        if (island.HasValue)
        {
            PlayerPrefs.SetInt("BoatTriviaDone_" + island.Value.ToString(), 1);
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Menavigasi dari Peta ke pulau tujuan: memutar Cutscene_BoatToIsland sekali di kunjungan pertama,
    /// atau langsung teleport ke scene pulau jika boat trivia sudah pernah dijalankan / pulau selesai.
    /// </summary>
    public static void NavigateToIsland(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[BoatTransition] sceneName kosong!");
            return;
        }

        if (!QuestJournalManager.CanLeaveCurrentIslandForMap())
        {
            Debug.LogWarning("[BoatTransition] Teleport dibatalkan karena seluruh quest pada pulau ini belum selesai.");
            return;
        }

        Time.timeScale = 1f;
        MapController.targetIslandScene = sceneName;

        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.targetIslandName = sceneName;
        }

        PlayerPrefs.SetString("TargetIsland", sceneName);
        PlayerPrefs.Save();

        if (HasCompletedBoatTriviaOrVisited(sceneName))
        {
            int buildIndex = FindTargetSceneBuildIndex(sceneName, out string resolvedSceneName);
            Debug.Log($"[BoatTransition] Boat trivia untuk '{sceneName}' sudah selesai/dilewati -> langsung pindah ke scene '{(string.IsNullOrEmpty(resolvedSceneName) ? sceneName : resolvedSceneName)}'.");
            if (buildIndex >= 0)
            {
                SceneManager.LoadScene(buildIndex);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
            return;
        }

        Debug.Log($"[BoatTransition] Kunjungan pertama ke '{sceneName}'. Memuat Cutscene_BoatToIsland.");
        SceneManager.LoadScene("Cutscene_BoatToIsland");
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
        int buildIndex = FindTargetSceneBuildIndex(currentTarget, out string resolvedSceneName);
        if (buildIndex < 0)
        {
            Debug.LogError($"[BoatTransition] Scene '{currentTarget}' tidak ditemukan di Build Settings. Tambahkan scene tersebut ke Scenes In Build dan pastikan namanya cocok.", this);
            return;
        }

        MarkBoatTriviaCompleted(currentTarget, resolvedSceneName);

        // Pindah ke scene pulau tujuan yang sesungguhnya
        SceneManager.LoadScene(buildIndex);
    }

    public static int FindTargetSceneBuildIndex(string rawTarget, out string resolvedSceneName)
    {
        resolvedSceneName = null;
        if (string.IsNullOrWhiteSpace(rawTarget)) return -1;

        string targetName = rawTarget.Trim();
        int separatorIndex = Mathf.Max(targetName.LastIndexOf('/'), targetName.LastIndexOf('\\'));
        if (separatorIndex >= 0)
            targetName = targetName.Substring(separatorIndex + 1);
        if (targetName.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
            targetName = targetName.Substring(0, targetName.Length - 6);

        string targetBase = ExtractBaseIslandName(targetName);
        int exactBaseIndex = -1;
        string exactBaseName = null;
        int fallbackIndex = -1;
        string fallbackName = null;

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            int pathSeparatorIndex = Mathf.Max(scenePath.LastIndexOf('/'), scenePath.LastIndexOf('\\'));
            string sceneName = scenePath.Substring(pathSeparatorIndex + 1);
            if (sceneName.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
                sceneName = sceneName.Substring(0, sceneName.Length - 6);

            // 1. Exact match
            if (string.Equals(sceneName, targetName, System.StringComparison.OrdinalIgnoreCase))
            {
                resolvedSceneName = sceneName;
                return i;
            }

            // 2. Exact base name match (misal "05_Sawinggrai" -> "06_Sawinggrai", bukan "09_Laut Sawinggrai")
            string sceneBase = ExtractBaseIslandName(sceneName);
            if (!string.IsNullOrEmpty(targetBase) && !string.IsNullOrEmpty(sceneBase))
            {
                if (exactBaseIndex < 0 && targetBase == sceneBase)
                {
                    exactBaseIndex = i;
                    exactBaseName = sceneName;
                }
                else if (fallbackIndex < 0 && (targetBase.Contains(sceneBase) || sceneBase.Contains(targetBase)))
                {
                    fallbackIndex = i;
                    fallbackName = sceneName;
                }
            }
        }

        if (exactBaseIndex >= 0)
        {
            resolvedSceneName = exactBaseName;
            return exactBaseIndex;
        }

        resolvedSceneName = fallbackName;
        return fallbackIndex;
    }

    private int FindTargetSceneBuildIndex()
    {
        return FindTargetSceneBuildIndex(currentTarget, out _);
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