using UnityEngine;
using UnityEngine.SceneManagement;

public class IslandCompletedStateRestorer : MonoBehaviour
{
    private static IslandCompletedStateRestorer instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null) return;

        GameObject host = new GameObject(nameof(IslandCompletedStateRestorer));
        DontDestroyOnLoad(host);
        instance = host.AddComponent<IslandCompletedStateRestorer>();
        RestoreActiveSceneState();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RestoreSceneState(scene);
    }

    /// <summary>
    /// Memulihkan state NPC, rintangan, dan dialog loop apabila pulau pada scene aktif sudah selesai.
    /// </summary>
    public static void RestoreActiveSceneState()
    {
        RestoreSceneState(SceneManager.GetActiveScene());
    }

    /// <summary>
    /// Memulihkan state akhir pulau yang sudah selesai pada scene tertentu.
    /// </summary>
    public static void RestoreSceneState(Scene scene)
    {
        if (!scene.IsValid()) return;

        IslandName? island = QuestJournalManager.GetIslandFromSceneName(scene.name);
        if (!island.HasValue) return;

        bool isCompleted = QuestJournalManager.IsIslandCompletedPersistent(island.Value) ||
                           PlayerPrefs.GetInt("GameCompleted", 0) == 1;
        if (!isCompleted) return;

        switch (island.Value)
        {
            case IslandName.Waisai:
                ApplyWaisaiCompletedState(scene);
                break;
            case IslandName.Piaynemo:
                ApplyPiaynemoCompletedState(scene);
                break;
            case IslandName.Arborek:
                ApplyArborekCompletedState(scene);
                break;
            case IslandName.Misool:
                ApplyMisoolCompletedState();
                break;
            case IslandName.Sawinggrai:
                ApplySawinggraiCompletedState(scene);
                break;
            case IslandName.Friwen:
                ApplyFriwenCompletedState(scene);
                break;
            case IslandName.Gag:
                ApplyGagCompletedState(scene);
                break;
        }

        RefreshAllSceneDialogueLoops(scene);
    }

    /// <summary>
    /// Mencari GameObject di scene tertentu baik dalam kondisi aktif maupun nonaktif.
    /// </summary>
    public static GameObject FindObjectInScene(Scene scene, string exactName)
    {
        if (!scene.IsValid() || string.IsNullOrEmpty(exactName)) return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] children = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < children.Length; j++)
            {
                if (children[j].name == exactName)
                {
                    return children[j].gameObject;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Mengubah status aktif daftar GameObject pada scene berdasarkan namanya.
    /// </summary>
    public static void SetObjectsActive(Scene scene, bool active, params string[] names)
    {
        if (names == null) return;

        for (int i = 0; i < names.Length; i++)
        {
            GameObject target = FindObjectInScene(scene, names[i]);
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }

    /// <summary>
    /// Memperbarui status dialog loop pada seluruh NPC non-peta di scene yang sudah selesai.
    /// </summary>
    public static void RefreshAllSceneDialogueLoops(Scene scene)
    {
        if (!scene.IsValid()) return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Dialogue[] dialogues = roots[i].GetComponentsInChildren<Dialogue>(true);
            for (int j = 0; j < dialogues.Length; j++)
            {
                if (dialogues[j] != null)
                {
                    dialogues[j].RefreshPostQuestLoopState();
                }
            }
        }
    }

    private static void ApplyWaisaiCompletedState(Scene scene)
    {
        SetObjectsActive(scene, false, "TourGuide", "AreaFoto_Sasi");
        SetObjectsActive(scene, true, "TourGuide_Trivia");
    }

    private static void ApplyPiaynemoCompletedState(Scene scene)
    {
        SetObjectsActive(scene, false, "TourGuide", "AreaFoto_Tebing");
        SetObjectsActive(scene, true, "TourGuide2");
    }

    private static void ApplyArborekCompletedState(Scene scene)
    {
        ArborekFaderManager manager = FindFirstObjectByType<ArborekFaderManager>();
        if (manager != null)
        {
            manager.ApplyCompletedState();
            return;
        }

        SetObjectsActive(scene, false,
            "NPC_TetuaAdat_Awal",
            "Umpan",
            "Kerang",
            "NPC_TetuaAdat_TerimaUmpan",
            "NPC_TetuaAdat_TerimaKarang",
            "NPC_TetuaAdat_Sasi_Before",
            "NPC_TetuaAdat_Sasi_After",
            "TitikSasi_1",
            "TitikSasi_2",
            "NPC_TetuaAdat_Fotografi",
            "TourGuide",
            "AreaFoto_Sasi");
    }

    private static void ApplyMisoolCompletedState()
    {
        MisoolLandSceneManager landManager = MisoolLandSceneManager.Instance != null
            ? MisoolLandSceneManager.Instance
            : FindFirstObjectByType<MisoolLandSceneManager>();
        if (landManager != null)
        {
            landManager.RefreshLandState();
        }
    }

    private static void ApplySawinggraiCompletedState(Scene scene)
    {
        SetObjectsActive(scene, false, "PohonObstacle", "Warlok1", "AreaFoto_Sasi");
        SetObjectsActive(scene, true, "Warlok1 (1)");
    }

    private static void ApplyFriwenCompletedState(Scene scene)
    {
        FriwenEventManager eventManager = FindFirstObjectByType<FriwenEventManager>();
        if (eventManager != null)
        {
            eventManager.ApplyCompletedState();
            return;
        }

        SetObjectsActive(scene, false,
            "Perahu_lepas1_Quest 1_0",
            "Perahu_Lepas2_Quest 1_0",
            "AreaFoto_Sasi",
            "Kerang");
        SetObjectsActive(scene, true,
            "Perahu_keiket_Quest 1_0 (1)",
            "NPC_Warlok1_QTE",
            "TourGuide");
    }

    private static void ApplyGagCompletedState(Scene scene)
    {
        GagFaderManager manager = FindFirstObjectByType<GagFaderManager>();
        if (manager != null)
        {
            manager.ApplyCompletedState();
            return;
        }

        SetObjectsActive(scene, false,
            "NPC_Peneliti_Awal",
            "NPC_Peneliti_Quest2",
            "NPC_Peneliti_Quest3",
            "NPC_Tetua_Lemas",
            "NPC_Tetua_Lemas2",
            "NPC_Tetua_Lemas3",
            "Item_Sagu",
            "Area_ViewTambang");
        SetObjectsActive(scene, true,
            "NPC_Tetua_Lemas4",
            "TourGuide");
    }
}
