using UnityEngine;

public class GagSceneManager : MonoBehaviour
{
    public static GagSceneManager Instance { get; private set; }
    private int lastObservedStage = -1;

    [Header("Referensi NPC Peneliti")]
    [Tooltip("Peneliti di awal pulau (Quest Sampel Air)")]
    [SerializeField] private GameObject penelitiAwal;

    [Tooltip("Peneliti pada Quest 2")]
    [SerializeField] private GameObject penelitiQuest2;

    [Tooltip("Peneliti pada Quest 3")]
    [SerializeField] private GameObject penelitiQuest3;

    [Header("Referensi Tetua Lemas")]
    [SerializeField] private GameObject tetuaLemas1;
    [SerializeField] private GameObject tetuaLemas2;
    [SerializeField] private GameObject tetuaLemas3;
    [SerializeField] private GameObject tetuaLemas4;

    [Header("Referensi NPC Kampanye / Akhir")]
    [Tooltip("NPC Kampanye lingkungan di akhir")]
    [SerializeField] private GameObject npcKampanye;

    [Header("Referensi NPC Locked & Unlocked")]
    [Tooltip("NPC saat quest masih terkunci")]
    [SerializeField] private GameObject npcLocked;

    [Tooltip("NPC saat quest sudah terbuka")]
    [SerializeField] private GameObject npcUnlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        AutoFindReferencesIfNull();
    }

    private void AutoFindReferencesIfNull()
    {
        if (penelitiAwal == null) penelitiAwal = GameObject.Find("NPC_Peneliti_Awal");
        if (penelitiQuest2 == null) penelitiQuest2 = GameObject.Find("NPC_Peneliti_Quest2");
        if (penelitiQuest3 == null) penelitiQuest3 = GameObject.Find("NPC_Peneliti_Quest3");

        if (tetuaLemas1 == null) tetuaLemas1 = GameObject.Find("NPC_Tetua_Lemas");
        if (tetuaLemas2 == null) tetuaLemas2 = GameObject.Find("NPC_Tetua_Lemas2");
        if (tetuaLemas3 == null) tetuaLemas3 = GameObject.Find("NPC_Tetua_Lemas3");
        if (tetuaLemas4 == null) tetuaLemas4 = GameObject.Find("NPC_Tetua_Lemas4");

        if (npcKampanye == null) npcKampanye = GameObject.Find("NPC_Kampanye");
    }

    private void Start()
    {
        if (MasterQuestManager.Instance != null)
        {
            MasterQuestManager.Instance.SetActiveIsland(IslandType.Gag);
            MasterQuestManager.Instance.EnsureStageInitialized();
            Debug.Log($"<color=cyan>[GagSceneManager] STAGE GAG SAAT INI: {MasterQuestManager.Instance.CurrentStage}</color>");
        }

        RefreshSceneState();
    }

    private void Update()
    {
        if (MasterQuestManager.Instance == null) return;

        int stage = MasterQuestManager.Instance.CurrentStage;
        if (stage != lastObservedStage)
        {
            RefreshSceneState();
        }
    }

    public void RefreshSceneState()
    {
        if (MasterQuestManager.Instance == null) return;

        int stage = MasterQuestManager.Instance.CurrentStage;
        lastObservedStage = stage;

        Debug.Log($"<color=cyan>[GagSceneManager] Refresh State Gag. Stage Aktif: {stage}</color>");

        // Stage 0 - 1: Peneliti Awal & Sampel Air
        if (penelitiAwal != null) penelitiAwal.SetActive(stage == 0 || stage == 1);
        if (tetuaLemas1 != null) tetuaLemas1.SetActive(stage == 1);

        // Stage 2: Krisis Air
        if (penelitiQuest2 != null) penelitiQuest2.SetActive(stage == 2);
        if (tetuaLemas2 != null) tetuaLemas2.SetActive(stage == 2);

        // Stage 3 - 4: Peneliti Lanjutan & Tetua
        if (penelitiQuest3 != null) penelitiQuest3.SetActive(stage == 3 || stage == 4);
        if (tetuaLemas3 != null) tetuaLemas3.SetActive(stage == 3);
        if (tetuaLemas4 != null) tetuaLemas4.SetActive(stage == 4);

        // Stage 5+: Kampanye / Ending Gag
        if (npcKampanye != null) npcKampanye.SetActive(stage >= 5);

        // NPC Locked / Unlocked
        if (npcLocked != null && npcUnlocked != null)
        {
            bool isUnlocked = stage >= 4 || QuestJournalManager.CanCompleteIslandOrOpenMap(IslandName.Gag);
            npcLocked.SetActive(!isUnlocked);
            npcUnlocked.SetActive(isUnlocked);
        }
    }

    public void AdvanceStageTo(int newStage)
    {
        if (MasterQuestManager.Instance != null)
        {
            MasterQuestManager.Instance.AdvanceStageTo(newStage);
        }
        else
        {
            RefreshSceneState();
        }
    }
}
