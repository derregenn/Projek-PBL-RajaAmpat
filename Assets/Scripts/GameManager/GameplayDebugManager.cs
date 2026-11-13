using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools Debug & Verification untuk Alpha Testing.
/// Tekan tombol F1 (atau klik tombol kecil di layar) untuk membuka panel debug in-game.
/// Memungkinkan teleport ke pulau mana pun, skip stage quest, membuka Pulau Gag (85%),
/// menyelesaikan quest seketika, dan reset data penyimpanan secara aman.
/// </summary>
public class GameplayDebugManager : MonoBehaviour
{
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

    private Rect windowRect = new Rect(20, 20, 360, 520);
    private Vector2 scrollPos;

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
        "Cutscene_BoatToIsland"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        // Pastikan Debug Manager selalu hadir otomatis di scene apa pun saat play di Unity
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
        if (Input.GetKeyDown(toggleKey))
        {
            showDebugWindow = !showDebugWindow;
        }
    }

    private void OnGUI()
    {
        // Tombol kecil di pojok kanan atas untuk layar sentuh / jika lupa hotkey F1
        if (GUI.Button(new Rect(Screen.width - 90, 10, 80, 26), showDebugWindow ? "Close [F1]" : "Debug [F1]"))
        {
            showDebugWindow = !showDebugWindow;
        }

        if (!showDebugWindow) return;

        GUI.backgroundColor = new Color(0.1f, 0.15f, 0.2f, 0.95f);
        windowRect = GUI.Window(9999, windowRect, DrawDebugWindow, "★ Eco-Tourism Alpha Debug Suite ★");
    }

    private void DrawDebugWindow(int windowID)
    {
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        // --- 1. STATUS RUNTIME ---
        GUILayout.Label("<b>[ STATUS SYSTEM ]</b>", GUI.skin.box);
        string currentScene = SceneManager.GetActiveScene().name;
        GUILayout.Label($"Scene Aktif: <b><color=cyan>{currentScene}</color></b>");

        float progress = GamePhaseManager.Instance != null ? GamePhaseManager.Instance.GetCurrentPercentage() : 0f;
        bool gagUnlocked = GamePhaseManager.Instance != null && GamePhaseManager.Instance.GetIsGagUnlocked();
        GUILayout.Label($"Progress Total: <b>{progress:F1}%</b> | Gag Unlocked: <b>{(gagUnlocked ? "<color=green>YES</color>" : "<color=red>NO</color>")}</b>");

        if (MasterQuestManager.Instance != null)
        {
            GUILayout.Label($"Pulau: <b>{MasterQuestManager.Instance.CurrentIsland}</b> | Stage: <b>{MasterQuestManager.Instance.CurrentStage}</b> | Items: <b>{MasterQuestManager.Instance.CollectibleCount}</b>");
        }

        if (QuestController.Instance != null)
        {
            int activeCount = QuestController.Instance.ActiveQuests.Count;
            GUILayout.Label($"Quest Aktif ({activeCount}):");
            foreach (var q in QuestController.Instance.ActiveQuests)
            {
                if (q != null) GUILayout.Label($" • {q.QuestTitle} ({q.QuestID})");
            }
        }

        GUILayout.Space(8);

        // --- 2. KONTROL STAGE QUEST ---
        GUILayout.Label("<b>[ KONTROL STAGE QUEST (MasterQuest) ]</b>", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Prev Stage (-1)"))
        {
            if (MasterQuestManager.Instance != null)
            {
                int s = Mathf.Max(0, MasterQuestManager.Instance.CurrentStage - 1);
                MasterQuestManager.Instance.AdvanceStageTo(s);
            }
        }
        if (GUILayout.Button("Next Stage (+1)"))
        {
            if (MasterQuestManager.Instance != null)
            {
                MasterQuestManager.Instance.AdvanceStageTo(MasterQuestManager.Instance.CurrentStage + 1);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        for (int i = 0; i <= 5; i++)
        {
            int stg = i;
            if (GUILayout.Button($"Stg {stg}"))
            {
                MasterQuestManager.Instance?.AdvanceStageTo(stg);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        for (int i = 6; i <= 9; i++)
        {
            int stg = i;
            if (GUILayout.Button($"Stg {stg}"))
            {
                MasterQuestManager.Instance?.AdvanceStageTo(stg);
            }
        }
        if (GUILayout.Button("+1 Bulu Seribu"))
        {
            MasterQuestManager.Instance?.AddBuluSeribu();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        // --- 3. PROGRES CERITA & CHEATS ---
        GUILayout.Label("<b>[ STORY PROGRESS & CHEATS ]</b>", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Buka Gag (85%)"))
        {
            GamePhaseManager.Instance?.SetPercentageDirectly(85f);
            GamePhaseManager.Instance?.UnlockGag(true);
        }
        if (GUILayout.Button("Tamat 100%"))
        {
            GamePhaseManager.Instance?.SetPercentageDirectly(100f);
            GamePhaseManager.Instance?.UnlockGag(true);
            StoryManager.Instance?.CompleteIsland(6);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Scene State"))
        {
            MasterQuestManager.Instance?.RefreshCurrentLandState();
        }
        if (GUILayout.Button("<color=red>Reset All Progress</color>"))
        {
            PlayerPrefs.DeleteAll();
            QuestJournalManager.Instance?.ResetAllProgress(true);
            GamePhaseManager.Instance?.ResetAllProgress();
            StoryManager.Instance?.ResetProgress();
            MasterQuestManager.Instance?.ResetAllProgress();
            QuestController.Instance?.ActiveQuests.Clear();
            Debug.LogWarning("[GameplayDebugManager] Seluruh data save telah direset!");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        // --- 4. FAST TELEPORT KE SCENE LAIN ---
        GUILayout.Label("<b>[ FAST TELEPORT / LOAD SCENE ]</b>", GUI.skin.box);
        foreach (string scn in allScenes)
        {
            if (GUILayout.Button($"Pindah ke: {scn}"))
            {
                Time.timeScale = 1f;
                MapController.targetIslandScene = scn;
                if (GamePhaseManager.Instance != null) GamePhaseManager.Instance.targetIslandName = scn;
                PlayerPrefs.SetString("TargetIsland", scn);
                PlayerPrefs.Save();
                SceneManager.LoadScene(scn);
            }
        }

        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0, 0, 10000, 30));
    }
}
