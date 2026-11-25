using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools Debug & Verification untuk Alpha Testing.
/// Tekan tombol F1 (atau klik tombol kecil di kiri atas layar) untuk membuka panel debug in-game.
/// Memungkinkan ubah stage, selesaikan quest/pulau, buka Pulau Gag (85%),
/// buka World Map, unfreeze player/kamera, fast teleport, dan reset save data.
/// </summary>
public class GameplayDebugManager : MonoBehaviour
{
    private const bool DebugMenuEnabled = true; // Set false untuk menonaktifkan menu debug di build final

    private static GameplayDebugManager _instance;
    public static GameplayDebugManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameplayDebugManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameplayDebugManager");
                    _instance = go.AddComponent<GameplayDebugManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("Hotkeys & UI")]
    public KeyCode toggleKey = KeyCode.F1;
    public bool showDebugWindow = false;

    private Rect windowRect = new Rect(16, 48, 400, 600);
    private Vector2 scrollPos;
    private bool useBoatCutsceneOnTeleport = false;
    private string lastActionMessage = "Siap untuk testing.";

    private readonly string[] allScenes = new string[]
    {
        "01_MainMenu",
        "02_CutsceneAirstrip",
        "02_Waisai",
        "03_Pianemo",
        "04_Misool",
        "05_Arborek",
        "06_Sawinggrai",
        "07_Friwen",
        "08_Laut Misool 1",
        "08_Laut Misool 2",
        "09_Laut Sawinggrai",
        "10_Gag",
        "11_Waisai Ending",
        "12_CutsceneAirstripEnding",
        "Cutscene_BoatToIsland"
    };

    private static readonly IslandName[] PrerequisiteIslands = new IslandName[]
    {
        IslandName.Waisai,
        IslandName.Piaynemo,
        IslandName.Misool,
        IslandName.Arborek,
        IslandName.Sawinggrai,
        IslandName.Friwen
    };

    private static readonly (IslandName island, string[] questIds, int finalStage)[] IslandCompletionDefs =
    {
        (IslandName.Airstrip,   new[] { "01_Airstrip" }, 1),
        (IslandName.Waisai,     new[] { "01_Waisai", "02_Waisai", "03_Waisai" }, 2),
        (IslandName.Piaynemo,   new[] { "01_Piaynemo", "02_Piaynemo", "03_Piaynemo", "04_Piaynemo", "05_Piaynemo", "06_Piaynemo", "06_Pianemo" }, 6),
        (IslandName.Misool,     new[] { "01_Misool", "02_Misool", "03_Misool", "04_Misool", "05_Misool", "06_Misool", "07_Misool", "08_Misool" }, 9),
        (IslandName.Arborek,    new[] { "01_Arborek", "02_Arborek", "03_Arborek", "04_Arborek", "05_Arborek", "06_Arborek" }, 7),
        (IslandName.Sawinggrai, new[] { "01_Sawinggrai", "02_Sawinggrai", "03_Sawinggrai", "04_Sawinggrai", "05_Sawinggrai" }, 7),
        (IslandName.Friwen,     new[] { "01_Friwen", "02_Friwen", "03_Friwen", "04_Friwen", "07", "08", "09", "10" }, 4),
        (IslandName.Gag,        new[] { "01_Gag", "02_Gag", "03_Gag", "04_Gag", "05_Gag", "06_Gag", "07_Gag", "08_Gag", "01", "02", "03", "04", "05", "06", "07", "08" }, 5)
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (!DebugMenuEnabled) return;

        if (_instance == null)
        {
            _ = Instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!DebugMenuEnabled) return;

        if (Input.GetKeyDown(toggleKey))
        {
            showDebugWindow = !showDebugWindow;
        }

        if (showDebugWindow)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void OnGUI()
    {
        if (!DebugMenuEnabled) return;

        GUI.backgroundColor = Color.white;
        GUI.contentColor = Color.white;

        // Kiri atas agar tidak bertabrakan dengan tombol HUD PAUSE/JOURNAL di kanan atas
        if (GUI.Button(new Rect(12, 12, 110, 28), showDebugWindow ? "Close [F1]" : "Debug [F1]"))
        {
            showDebugWindow = !showDebugWindow;
        }

        if (!showDebugWindow) return;

        float maxWidth = Mathf.Min(420f, Mathf.Max(280f, Screen.width - 24f));
        float maxHeight = Mathf.Min(640f, Mathf.Max(260f, Screen.height - 60f));
        windowRect.width = maxWidth;
        windowRect.height = maxHeight;
        windowRect.x = Mathf.Clamp(windowRect.x, 8f, Mathf.Max(8f, Screen.width - windowRect.width - 8f));
        windowRect.y = Mathf.Clamp(windowRect.y, 44f, Mathf.Max(44f, Screen.height - windowRect.height - 8f));

        GUI.color = Color.white;
        GUI.backgroundColor = new Color(0.2f, 0.25f, 0.32f, 0.96f);
        windowRect = GUI.Window(9999, windowRect, DrawDebugWindow, "★ Eco-Tourism Alpha Debug Suite ★");
    }

    private void DrawDebugWindow(int windowID)
    {
        GUI.backgroundColor = Color.white;
        GUI.contentColor = Color.white;

        scrollPos = GUILayout.BeginScrollView(scrollPos);

        // --- 1. STATUS RUNTIME ---
        GUILayout.Label("<b>[ STATUS SYSTEM ]</b>", GUI.skin.box);
        string currentScene = SceneManager.GetActiveScene().name;
        GUILayout.Label($"Scene Aktif: <b><color=cyan>{currentScene}</color></b>");

        float progress = GamePhaseManager.Instance != null ? GamePhaseManager.Instance.GetCurrentPercentage() : 0f;
        bool gagUnlocked = GamePhaseManager.IsGagUnlockedPersistent();
        bool allPrereqDone = QuestJournalManager.AreAllPrerequisiteIslandsCompleted();
        int trashCount = PlayerPrefs.GetInt("TotalTrashCollected", TrashInteractable.totalCollected);
        GUILayout.Label($"Progress: <b>{progress:F1}%</b> | Gag: <b>{(gagUnlocked ? "<color=lime>YES</color>" : "<color=orange>NO</color>")}</b> | 6 Pulau: <b>{(allPrereqDone ? "<color=lime>DONE</color>" : "NO")}</b> | Trash: <b>{trashCount}/20</b>");

        if (MasterQuestManager.Instance != null)
        {
            GUILayout.Label($"Pulau: <b>{MasterQuestManager.Instance.CurrentIsland}</b> | Stage: <b>{MasterQuestManager.Instance.CurrentStage}</b> | Collect: <b>{MasterQuestManager.Instance.CollectibleCount}</b>");
        }

        GUILayout.Label($"<color=yellow>Info: {lastActionMessage}</color>");

        if (QuestController.Instance != null)
        {
            int activeCount = QuestController.Instance.ActiveQuests.Count;
            GUILayout.Label($"Quest Aktif ({activeCount}):");
            foreach (var q in QuestController.Instance.ActiveQuests)
            {
                if (q != null) GUILayout.Label($" • {q.QuestTitle} ({q.QuestID})");
            }
        }

        GUILayout.Space(6);

        // --- 2. KONTROL STAGE QUEST ---
        GUILayout.Label("<b>[ KONTROL STAGE QUEST (MasterQuest) ]</b>", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Prev Stage (-1)"))
        {
            MasterQuestManager.Instance.DetectIslandFromActiveScene(currentScene);
            int target = Mathf.Max(0, MasterQuestManager.Instance.CurrentStage - 1);
            ApplyDebugStage(target);
        }
        if (GUILayout.Button("Next Stage (+1)"))
        {
            MasterQuestManager.Instance.DetectIslandFromActiveScene(currentScene);
            int target = MasterQuestManager.Instance.CurrentStage + 1;
            ApplyDebugStage(target);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        for (int i = 0; i <= 4; i++)
        {
            int stg = i;
            if (GUILayout.Button($"Stg {stg}"))
            {
                ApplyDebugStage(stg);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        for (int i = 5; i <= 9; i++)
        {
            int stg = i;
            if (GUILayout.Button($"Stg {stg}"))
            {
                ApplyDebugStage(stg);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+1 Progress Objective"))
        {
            ProgressActiveObjectiveDebug();
        }
        if (GUILayout.Button("Selesaikan Quest Aktif"))
        {
            CompleteActiveQuestsDebug();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+1 Bulu Seribu"))
        {
            MasterQuestManager.Instance.AddBuluSeribu();
            MasterQuestManager.Instance.RefreshCurrentLandState();
            SetStatus($"Bulu Seribu -> {MasterQuestManager.Instance.CollectibleCount}/3 (Stage {MasterQuestManager.Instance.CurrentStage})");
        }
        if (GUILayout.Button("Sampah +1"))
        {
            SetTrashCountDebug(trashCount + 1);
        }
        if (GUILayout.Button("Sampah 20/20"))
        {
            SetTrashCountDebug(20);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        // --- 3. PROGRES CERITA & CHEATS ---
        GUILayout.Label("<b>[ STORY PROGRESS, MAP & UNSTUCK ]</b>", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Selesaikan Pulau Ini"))
        {
            CompleteCurrentIslandDebug();
        }
        if (GUILayout.Button("Buka Gag (85%)"))
        {
            UnlockGagAndAllPrerequisitesDebug();
        }
        if (GUILayout.Button("Tamat 100%"))
        {
            CompleteAll100PercentDebug();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Buka World Map"))
        {
            OpenWorldMapDebug();
        }
        if (GUILayout.Button("Refresh Scene State"))
        {
            MasterQuestManager.Instance.DetectIslandFromActiveScene(currentScene);
            MasterQuestManager.Instance.RefreshCurrentLandState();
            RefreshAllGagUnlockers();
            SetStatus("Scene state & Gag Map di-refresh.");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Unfreeze Player & Cam"))
        {
            UnfreezePlayerAndCameraDebug();
        }
        GUI.backgroundColor = new Color(1f, 0.45f, 0.45f, 1f);
        if (GUILayout.Button("Reset All + Reload"))
        {
            GUI.backgroundColor = Color.white;
            ResetAllProgressAndReloadDebug();
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            return;
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        // --- 4. FAST TELEPORT KE SCENE LAIN ---
        GUILayout.Label("<b>[ FAST TELEPORT / LOAD SCENE ]</b>", GUI.skin.box);
        useBoatCutsceneOnTeleport = GUILayout.Toggle(useBoatCutsceneOnTeleport, " Pakai Cutscene Boat Trivia saat Teleport");
        foreach (string scn in allScenes)
        {
            if (GUILayout.Button($"Pindah ke: {scn}"))
            {
                TeleportToSceneDebug(scn);
            }
        }

        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0, 0, 10000, 30));
    }

    private void SetStatus(string msg)
    {
        lastActionMessage = msg;
        Debug.Log($"[GameplayDebugManager] {msg}");
    }

    private static int GetFinalStageForIsland(IslandName island)
    {
        for (int i = 0; i < IslandCompletionDefs.Length; i++)
        {
            if (IslandCompletionDefs[i].island == island)
            {
                return IslandCompletionDefs[i].finalStage;
            }
        }
        return 5;
    }

    private void ApplyDebugStage(int targetStage)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        MasterQuestManager mqm = MasterQuestManager.Instance;
        mqm.DetectIslandFromActiveScene(sceneName);

        IslandName? activeIsland = QuestJournalManager.GetIslandFromSceneName(sceneName);
        if (activeIsland.HasValue && targetStage < GetFinalStageForIsland(activeIsland.Value))
        {
            ClearIslandCompletionFlags(activeIsland.Value);
        }

        mqm.AdvanceStageTo(targetStage);
        SyncScenePropsForStage(sceneName, targetStage);
        SetStatus($"{mqm.CurrentIsland} -> Stage {targetStage}");
    }

    private static void ClearIslandCompletionFlags(IslandName island)
    {
        if (QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.DebugResetIslandCompletion(island);
        }
        else
        {
            PlayerPrefs.DeleteKey("QJM_Award_" + island.ToString());
            PlayerPrefs.DeleteKey("GPM_Island_" + island.ToString());
            PlayerPrefs.DeleteKey("Island_" + (int)island);
            PlayerPrefs.DeleteKey("GameCompleted");
            if (island == IslandName.Misool) PlayerPrefs.DeleteKey("Misool_QuestStage");
            PlayerPrefs.Save();
            QuestJournalManager.InvalidatePersistentQuestCache();
        }
    }

    private static void SyncScenePropsForStage(string sceneName, int stage)
    {
        if (sceneName.Contains("Friwen"))
        {
            FriwenEventManager fem = FindFirstObjectByType<FriwenEventManager>(FindObjectsInactive.Include);
            if (fem != null)
            {
                if (fem.perahuLepas != null) fem.perahuLepas.SetActive(stage <= 1);
                if (fem.perahuKeikat != null) fem.perahuKeikat.SetActive(stage >= 2);
                if (fem.perahuMenjauh != null) fem.perahuMenjauh.SetActive(false);
                if (fem.npcWarlok1 != null) fem.npcWarlok1.SetActive(stage >= 2);
                if (fem.tourGuide != null) fem.tourGuide.SetActive(stage >= 3);
            }
            GameObject areaFoto = FindInActiveScene("AreaFoto_Sasi");
            if (areaFoto != null) areaFoto.SetActive(stage == 2 || stage == 3);
        }
        else if (sceneName.Contains("Arborek"))
        {
            SetActiveInScene("Umpan", stage <= 1);
            SetActiveInScene("Kerang", stage <= 2);
            SetActiveInScene("TitikSasi_1", stage == 3);
            SetActiveInScene("TitikSasi_2", stage == 3);
            SetActiveInScene("AreaFoto_Sasi", stage == 6);
        }
    }

    private void ProgressActiveObjectiveDebug()
    {
        QuestController qc = QuestController.Instance;
        if (qc == null)
        {
            SetStatus("QuestController tidak tersedia.");
            return;
        }

        Quest activeQuest = qc.GetCurrentActiveQuest();
        if (activeQuest == null || activeQuest.Objectives == null)
        {
            SetStatus("Tidak ada quest aktif saat ini.");
            return;
        }

        foreach (QuestObjective obj in activeQuest.Objectives)
        {
            if (obj != null && !obj.IsComplete && !string.IsNullOrEmpty(obj.ObjectiveID))
            {
                qc.ProgressObjective(obj.ObjectiveID, 1);
                SetStatus($"Progress '{obj.ObjectiveID}' +1.");
                return;
            }
        }

        qc.CompleteAllActiveQuests();
        SetStatus($"Quest '{activeQuest.QuestTitle}' selesai.");
    }

    private void CompleteActiveQuestsDebug()
    {
        QuestController qc = QuestController.Instance;
        if (qc == null || !qc.HasActiveQuest())
        {
            SetStatus("Tidak ada quest aktif.");
            return;
        }

        int count = qc.ActiveQuests.Count;
        qc.CompleteAllActiveQuests();
        SetStatus($"{count} quest aktif diselesaikan.");
    }

    private void SetTrashCountDebug(int count)
    {
        int clamped = Mathf.Clamp(count, 0, 20);
        TrashInteractable.totalCollected = clamped;
        PlayerPrefs.SetInt("TotalTrashCollected", clamped);
        PlayerPrefs.Save();
        QuestJournalManager.Instance?.UpdateAllJournalSystems();
        SetStatus($"Trash diset ke {clamped}/20.");
    }

    private void CompleteCurrentIslandDebug()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        IslandName? island = QuestJournalManager.GetIslandFromSceneName(sceneName);
        if (!island.HasValue)
        {
            SetStatus($"Scene '{sceneName}' tidak memetakan ke pulau quest.");
            return;
        }

        MarkIslandCompletedPersistently(island.Value);

        if (QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.DebugCompleteIsland(island.Value);
        }

        MasterQuestManager mqm = MasterQuestManager.Instance;
        mqm.DetectIslandFromActiveScene(sceneName);
        mqm.AdvanceStageTo(GetFinalStageForIsland(island.Value));

        GamePhaseManager.Instance.CompleteIsland(island.Value);
        UnlockSceneNPCMap();
        RefreshAllGagUnlockers();
        SetStatus($"Pulau {island.Value} selesai & NPCMap terbuka!");
    }

    private void UnlockGagAndAllPrerequisitesDebug()
    {
        for (int i = 0; i < PrerequisiteIslands.Length; i++)
        {
            IslandName isl = PrerequisiteIslands[i];
            MarkIslandCompletedPersistently(isl);
            if (QuestJournalManager.Instance != null)
            {
                QuestJournalManager.Instance.DebugCompleteIsland(isl);
            }
            GamePhaseManager.Instance.CompleteIsland(isl);
        }

        PlayerPrefs.SetInt("QJM_JournalUnlocked", 1);
        PlayerPrefs.Save();
        CanvasHUDAutoBinder.Instance?.RefreshJournalButtonState();

        GamePhaseManager.Instance.UnlockGag(true, true);
        GamePhaseManager.Instance.CalculatePercentage();
        if (GamePhaseManager.Instance.GetCurrentPercentage() < 85f)
        {
            GamePhaseManager.Instance.SetPercentageDirectly(85f);
        }

        UnlockSceneNPCMap();
        RefreshAllGagUnlockers();
        SetStatus("6 Pulau selesai -> Pulau Gag (85%) & NPCMap terbuka!");
    }

    private void CompleteAll100PercentDebug()
    {
        for (int i = 0; i < IslandCompletionDefs.Length; i++)
        {
            IslandName isl = IslandCompletionDefs[i].island;
            MarkIslandCompletedPersistently(isl);
            GamePhaseManager.Instance.CompleteIsland(isl);
        }

        TrashInteractable.totalCollected = 20;
        PlayerPrefs.SetInt("TotalTrashCollected", 20);
        PlayerPrefs.SetInt("Sasi_Count", 2);
        PlayerPrefs.SetInt("Visited_10_Gag", 1);
        PlayerPrefs.SetInt("GameCompleted", 1);
        PlayerPrefs.SetInt("QJM_JournalUnlocked", 1);
        PlayerPrefs.Save();

        if (QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.DebugUnlockAllJournalContent();
        }

        GamePhaseManager.Instance.UnlockGag(true, true);
        GamePhaseManager.Instance.SetPercentageDirectly(100f);
        StoryManager.Instance.CompleteIsland(6);
        CanvasHUDAutoBinder.Instance?.RefreshJournalButtonState();
        UnlockSceneNPCMap();
        RefreshAllGagUnlockers();
        SetStatus("Semua pulau 100%, Journal & Secret Ending terbuka!");
    }

    private void OpenWorldMapDebug()
    {
        MapController mc = FindFirstObjectByType<MapController>(FindObjectsInactive.Include);
        if (mc == null || mc.worldMapCanvasGroup == null)
        {
            SetStatus("MapController / MapMenuPanel tidak ada di scene ini.");
            return;
        }

        mc.EnsureStandardMapPanelSetup();
        mc.UpdateCurrentLocationMarker();
        RefreshAllGagUnlockers();

        CanvasGroup cg = mc.worldMapCanvasGroup;
        cg.gameObject.SetActive(true);
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
        Time.timeScale = 0f;
        SetStatus("World Map dibuka untuk testing.");
    }

    private void UnfreezePlayerAndCameraDebug()
    {
        Time.timeScale = 1f;

        Player[] landPlayers = FindObjectsByType<Player>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < landPlayers.Length; i++)
        {
            if (landPlayers[i] == null) continue;
            landPlayers[i].isMovementLocked = false;
            landPlayers[i].enabled = true;
            Rigidbody2D rb = landPlayers[i].GetComponent<Rigidbody2D>();
            if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.linearVelocity = Vector2.zero;
            }
        }

        PlayerSwim[] swimPlayers = FindObjectsByType<PlayerSwim>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < swimPlayers.Length; i++)
        {
            if (swimPlayers[i] != null) swimPlayers[i].isMovementLocked = false;
        }

        if (TriviaQuizUI.Instance != null && TriviaQuizUI.IsAnyQuizActive)
        {
            TriviaQuizUI.Instance.CloseQuiz();
        }

        CinemachineCamera[] vcams = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < vcams.Length; i++)
        {
            if (vcams[i] == null) continue;
            string n = vcams[i].name;
            if (n.IndexOf("Dialogue", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("ViewPerahu", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                vcams[i].Priority.Value = 0;
            }
            else if (string.Equals(n, "CinemachineCamera", StringComparison.OrdinalIgnoreCase))
            {
                vcams[i].Priority.Value = 10;
            }
        }

        SetStatus("Player, TimeScale, dan Kamera di-unfreeze.");
    }

    private void ResetAllProgressAndReloadDebug()
    {
        Time.timeScale = 1f;
        if (QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.ResetAllProgress(true);
        }

        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetInt("FreshTestPlay_Reset_v2", 1);
        PlayerPrefs.Save();
        QuestJournalManager.InvalidatePersistentQuestCache();

        TrashInteractable.totalCollected = 0;
        GlobalTrashManager.Instance?.ResetAll();
        GamePhaseManager.Instance.ResetAllProgress();
        StoryManager.Instance.ResetProgress();
        MasterQuestManager.Instance.ResetAllProgress();
        QuestController.Instance.ActiveQuests.Clear();

        SetStatus("Progress direset -> Reloading scene...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void TeleportToSceneDebug(string sceneName)
    {
        Time.timeScale = 1f;
        MapController.targetIslandScene = sceneName;
        GamePhaseManager.Instance.targetIslandName = sceneName;
        PlayerPrefs.SetString("TargetIsland", sceneName);
        PlayerPrefs.Save();

        if (useBoatCutsceneOnTeleport)
        {
            PlayerPrefs.SetInt("Visited_" + sceneName, 0);
            IslandName? isl = QuestJournalManager.GetIslandFromSceneName(sceneName);
            if (isl.HasValue) PlayerPrefs.SetInt("BoatTriviaDone_" + isl.Value.ToString(), 0);
            PlayerPrefs.Save();
            SceneManager.LoadScene("Cutscene_BoatToIsland");
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private static void MarkIslandCompletedPersistently(IslandName island)
    {
        PlayerPrefs.SetInt("QJM_Award_" + island.ToString(), 1);
        PlayerPrefs.SetInt("GPM_Island_" + island.ToString(), 1);
        PlayerPrefs.SetInt("Island_" + (int)island, 1);

        int storyIdx = StoryManager.IslandNameToStoryIndex(island);
        if (storyIdx >= 0)
        {
            PlayerPrefs.SetInt("Story_Island_" + storyIdx, 1);
            StoryManager.Instance.CompleteIsland(storyIdx);
        }

        if (island == IslandName.Misool)
        {
            PlayerPrefs.SetInt("Misool_QuestStage", 9);
            PlayerPrefs.SetInt("Misool_QuestStage", 9);
        }

        string existingQjm = PlayerPrefs.GetString("QJM_CompletedQuestIDs", "");
        var qSet = new HashSet<string>(existingQjm.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
        for (int i = 0; i < IslandCompletionDefs.Length; i++)
        {
            if (IslandCompletionDefs[i].island != island) continue;
            PlayerPrefs.SetInt($"{island}_QuestStage", IslandCompletionDefs[i].finalStage);
            foreach (string qid in IslandCompletionDefs[i].questIds)
            {
                qSet.Add(qid);
                GamePhaseManager.Instance.HandleQuestCompleted(qid);
            }
        }

        PlayerPrefs.SetString("QJM_CompletedQuestIDs", string.Join(",", qSet));
        PlayerPrefs.SetString("GPM_CompletedQuests", string.Join(",", qSet));
        PlayerPrefs.Save();
        QuestJournalManager.InvalidatePersistentQuestCache();
    }

    private static void UnlockSceneNPCMap()
    {
        SetActiveInScene("NPCMap", true);
        SetActiveInScene("NPC_Unlocked", true);
        SetActiveInScene("NPCMap_Locked", false);
        SetActiveInScene("NPC_Locked", false);
    }

    private static void RefreshAllGagUnlockers()
    {
        MapGagUnlocker[] unlockers = FindObjectsByType<MapGagUnlocker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < unlockers.Length; i++)
        {
            if (unlockers[i] != null) unlockers[i].RefreshGagState();
        }
    }

    private static GameObject FindInActiveScene(string exactName)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()) return null;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] trs = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < trs.Length; j++)
            {
                if (trs[j].name == exactName) return trs[j].gameObject;
            }
        }
        return null;
    }

    private static void SetActiveInScene(string exactName, bool active)
    {
        GameObject obj = FindInActiveScene(exactName);
        if (obj != null && obj.activeSelf != active)
        {
            obj.SetActive(active);
        }
    }
}
