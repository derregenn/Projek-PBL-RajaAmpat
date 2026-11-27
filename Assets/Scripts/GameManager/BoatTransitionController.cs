using UnityEngine;
using UnityEngine.SceneManagement;

public class BoatTransitionController : MonoBehaviour
{
    [Header("Referensi Sistem")]
    public Dialogue dialogueSystem;
    public TriviaQuizUI triviaSystem;

    [Header("Camera Settings (Inspector)")]
    [SerializeField] private float boatCameraLensSize = 3.6f;
    [SerializeField] private Vector3 boatCameraFollowOffset = new Vector3(0f, 1.4f, -10f);

    [Header("Lokasi Spawn Background")]
    [Tooltip("Pilih objek Environment sebagai tempat munculnya background parallax")]
    public Transform environmentParent; // Drag objek 'Environment' dari Hierarchy ke sini

    [Header("Data Pulau (Isi di Inspector)")]
    public IslandTransitionData[] islandData;

    private string currentTarget;
    private IslandTransitionData currentData;
    private readonly System.Collections.Generic.List<GameObject> cachedInteractPrompts = new System.Collections.Generic.List<GameObject>();

    private void Start()
    {
        // Pastikan Player & NPC di-anchor ke kapal dan kontrol player dinonaktifkan
        BoatBobbing bobbing = FindFirstObjectByType<BoatBobbing>();
        if (bobbing != null)
        {
            bobbing.InitializeAnchoring();
        }

        SetupCamera();

        // Nonaktifkan semua InteractPrompt di scene agar tidak melayang di langit selama cutscene perahu
        cachedInteractPrompts.Clear();
        var prompts = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in prompts)
        {
            if (p != null && p.name == "InteractPrompt")
            {
                cachedInteractPrompts.Add(p.gameObject);
                p.gameObject.SetActive(false);
            }
        }

        if (triviaSystem == null) triviaSystem = TriviaQuizUI.FindOrActivateInstance();
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

    private void SetupCamera()
    {
        var vcams = FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        BoatBobbing boat = FindFirstObjectByType<BoatBobbing>();
        Transform targetFollow = boat != null ? boat.transform : null;

        foreach (var vcam in vcams)
        {
            if (vcam != null)
            {
                if (targetFollow != null)
                {
                    vcam.Target.TrackingTarget = targetFollow;
                }

                var lens = vcam.Lens;
                lens.OrthographicSize = boatCameraLensSize > 0f ? boatCameraLensSize : 3.6f;
                vcam.Lens = lens;

                var follow = vcam.GetComponent<Unity.Cinemachine.CinemachineFollow>();
                if (follow != null)
                {
                    follow.FollowOffset = boatCameraFollowOffset;
                }

                var confiner = vcam.GetComponent<Unity.Cinemachine.CinemachineConfiner2D>();
                if (confiner != null)
                {
                    if (confiner.BoundingShape2D != null)
                    {
                        confiner.enabled = true;
                        confiner.SlowingDistance = 0f;
                        confiner.InvalidateBoundingShapeCache();
                    }
                    else
                    {
                        confiner.enabled = false;
                    }
                }

                if (vcam.name.IndexOf("Dialogue", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    vcam.Priority.Value = 0;
                }
            }
        }
    }

    private void LateUpdate()
    {
        for (int i = cachedInteractPrompts.Count - 1; i >= 0; i--)
        {
            GameObject promptObj = cachedInteractPrompts[i];
            if (promptObj == null)
            {
                cachedInteractPrompts.RemoveAt(i);
                continue;
            }
            if (promptObj.activeSelf)
            {
                promptObj.SetActive(false);
            }
        }
    }

    public static bool DebugForceNextBoatTrivia = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDebugForceFlag()
    {
        DebugForceNextBoatTrivia = false;
    }

    private void LoadIslandDataAndStart()
    {
        if (DebugForceNextBoatTrivia)
        {
            DebugForceNextBoatTrivia = false;
        }
        else if (HasCompletedBoatTriviaOrVisited(currentTarget))
        {
            Debug.Log($"[BoatTransition] Target '{currentTarget}' sudah pernah melewati boat trivia / pulau sudah selesai / mode Free Roam aktif. Langsung menuju pulau.");
            FinishTransition();
            return;
        }

        if (islandData != null)
        {
            string targetBase = ExtractBaseIslandName(currentTarget);

            foreach (var data in islandData)
            {
                if (data != null && !string.IsNullOrEmpty(data.islandName))
                {
                    string targetClean = currentTarget.ToLower().Trim();
                    string dataClean = data.islandName.ToLower().Trim();
                    string dataBase = ExtractBaseIslandName(data.islandName);

                    if (targetClean == dataClean ||
                        targetClean.Contains(dataClean) || dataClean.Contains(targetClean) ||
                        (!string.IsNullOrEmpty(targetBase) && !string.IsNullOrEmpty(dataBase) &&
                         (targetBase == dataBase || targetBase.Contains(dataBase) || dataBase.Contains(targetBase))))
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
            SpawnParallaxBackground();
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
        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"^\d+_", "");
        clean = clean.Replace("quests", "").Replace("quest", "").Replace("desa", "").Trim();
        clean = clean.Replace("pianemo", "piaynemo");
        return clean;
    }

    /// <summary>
    /// Mengecek apakah cutscene boat trivia untuk pulau tujuan sudah pernah diselesaikan,
    /// pulau sudah selesai/award terbuka, atau mode Free Roam sudah aktif.
    /// </summary>
    public static bool HasCompletedBoatTriviaOrVisited(string targetSceneOrIsland)
    {
        if (string.IsNullOrWhiteSpace(targetSceneOrIsland)) return false;

        if (PlayerPrefs.GetInt("GTM_FreeRoamMode", 0) == 1 ||
            (GlobalTrashManager.Instance != null && GlobalTrashManager.Instance.IsFreeRoamMode) ||
            FreeRoamManager.IsFreeRoamUnlocked())
        {
            return true;
        }

        FindTargetSceneBuildIndex(targetSceneOrIsland, out string resolvedSceneName);

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

            if (QuestJournalManager.IsIslandCompletedPersistent(island.Value) ||
                QuestJournalManager.IsPrerequisiteIslandCompleted(island.Value))
            {
                PlayerPrefs.SetInt("BoatTriviaDone_" + island.Value.ToString(), 1);
                PlayerPrefs.Save();
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

        if (ExtractBaseIslandName(sceneName) == "gag" && !GamePhaseManager.IsGagUnlockedPersistent())
        {
            Debug.LogWarning("[BoatTransition] Teleport ke 10_Gag dibatalkan karena seluruh pulau prasyarat belum selesai.");
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

    private void SpawnParallaxBackground()
    {
        if (environmentParent == null)
        {
            Debug.LogWarning("[BoatTransition] Environment Parent belum di-assign di Inspector.");
            return;
        }

        // 1. Aktifkan background yang sudah ada di bawah Environment dan matikan yang lain
        string targetBase = ExtractBaseIslandName(currentTarget);
        Transform matchedChild = null;

        foreach (Transform child in environmentParent)
        {
            if (child.name.StartsWith("BG") || child.name.StartsWith("Background"))
            {
                string childBase = ExtractBaseIslandName(child.name);
                if (!string.IsNullOrEmpty(targetBase) && !string.IsNullOrEmpty(childBase) &&
                    (childBase.Contains(targetBase) || targetBase.Contains(childBase)))
                {
                    matchedChild = child;
                }
                child.gameObject.SetActive(false);
            }
        }

        if (matchedChild != null)
        {
            matchedChild.gameObject.SetActive(true);
            Debug.Log($"[BoatTransition] Mengaktifkan background yang sudah ada: '{matchedChild.name}'");
            return;
        }

        // 2. Fallback: jika background belum ada di hierarchy Environment, spawn dari prefab Inspector
        if (currentData != null && currentData.islandBackgroundPrefab != null)
        {
            GameObject bgObj = currentData.islandBackgroundPrefab;
            if (bgObj.transform.IsChildOf(environmentParent))
            {
                bgObj.SetActive(true);
            }
            else
            {
                Vector3 originalLocalPos = bgObj.transform.localPosition;
                GameObject spawnedBG = Instantiate(bgObj, environmentParent);
                spawnedBG.transform.localPosition = new Vector3(0f, originalLocalPos.y, originalLocalPos.z);
                spawnedBG.SetActive(true);
            }
        }
        else
        {
            Debug.LogWarning("[BoatTransition] Prefab Background tidak ditemukan untuk target: " + currentTarget);
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
        TriviaQuizUI activeQuizUI = triviaSystem != null ? triviaSystem : TriviaQuizUI.FindOrActivateInstance();
        if (activeQuizUI != null && currentData != null && currentData.islandTriviaQuestions != null && currentData.islandTriviaQuestions.Length > 0)
        {
            activeQuizUI.StartQuiz(currentData.islandTriviaQuestions, FinishTransition);
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