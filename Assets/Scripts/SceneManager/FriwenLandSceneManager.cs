using UnityEngine;

public class FriwenLandSceneManager : MonoBehaviour
{
    public static FriwenLandSceneManager Instance { get; private set; }
    private int lastObservedStage = -1;

    [Header("Referensi Quest 1 (Tarik Perahu QTE)")]
    [Tooltip("Warlok 1 yang meminta bantuan menarik perahu")]
    [SerializeField] private GameObject warlok1Qte;

    [Tooltip("Trigger QTE untuk menarik perahu")]
    [SerializeField] private GameObject triggerQtePerahu;

    [Header("Referensi Quest 2 (Trivia Friwen)")]
    [Tooltip("Warlok 2 untuk sesi Trivia Friwen")]
    [SerializeField] private GameObject warlok2;

    [Header("Referensi Quest 3 & 4 (Foto Pasir Timbul & Friwen Wall)")]
    [Tooltip("Tour Guide di spot Pasir Timbul")]
    [SerializeField] private GameObject tourGuidePasirTimbul;

    [Tooltip("NPC Warlok opsional di pesisir")]
    [SerializeField] private GameObject warlokOpsional1;
    [SerializeField] private GameObject warlokOpsional2;

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
        if (warlok1Qte == null) warlok1Qte = GameObject.Find("NPC_Warlok1_QTE");
        if (triggerQtePerahu == null) triggerQtePerahu = GameObject.Find("Trigger_QTE_Perahu");

        if (warlok2 == null) warlok2 = GameObject.Find("NPC_Warlok2");
        if (tourGuidePasirTimbul == null) tourGuidePasirTimbul = GameObject.Find("Spot_TourGuide_PasirTimbul");

        if (warlokOpsional1 == null) warlokOpsional1 = GameObject.Find("NPC_warlok_opsional");
        if (warlokOpsional2 == null) warlokOpsional2 = GameObject.Find("NPC_warlok_opsional (1)");
    }

    private void Start()
    {
        if (MasterQuestManager.Instance != null)
        {
            MasterQuestManager.Instance.SetActiveIsland(IslandType.Friwen);
            MasterQuestManager.Instance.EnsureStageInitialized();
            Debug.Log($"<color=cyan>[FriwenLandSceneManager] STAGE FRIWEN SAAT INI: {MasterQuestManager.Instance.CurrentStage}</color>");
        }

        RefreshLandState();
    }

    private void Update()
    {
        if (MasterQuestManager.Instance == null) return;

        int stage = MasterQuestManager.Instance.CurrentStage;
        if (stage != lastObservedStage)
        {
            RefreshLandState();
        }
    }

    public void RefreshLandState()
    {
        if (MasterQuestManager.Instance == null) return;

        int stage = MasterQuestManager.Instance.CurrentStage;
        lastObservedStage = stage;

        Debug.Log($"<color=cyan>[FriwenLandSceneManager] Refresh State Friwen. Stage Aktif: {stage}</color>");

        // Stage 1: QTE Tarik Perahu
        if (warlok1Qte != null) warlok1Qte.SetActive(stage == 0 || stage == 1);
        if (triggerQtePerahu != null) triggerQtePerahu.SetActive(stage == 0 || stage <= 3);

        // Stage 1 - 2: Trivia Friwen bersama Warlok 2
        if (warlok2 != null) warlok2.SetActive(stage >= 1);

        // Stage 2 - 3: Foto Pasir Timbul
        if (tourGuidePasirTimbul != null) tourGuidePasirTimbul.SetActive(stage >= 2);

        // Warlok opsional
        if (warlokOpsional1 != null) warlokOpsional1.SetActive(true);
        if (warlokOpsional2 != null) warlokOpsional2.SetActive(true);

        // NPC Locked / Unlocked
        if (npcLocked != null && npcUnlocked != null)
        {
            bool isUnlocked = stage >= 3; // Misal, NPC terbuka setelah stage
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
            RefreshLandState();
        }
    }
}
