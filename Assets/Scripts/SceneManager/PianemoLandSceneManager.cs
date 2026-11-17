using UnityEngine;

public class PianemoLandSceneManager : MonoBehaviour
{
    public static PianemoLandSceneManager Instance { get; private set; }
    private int lastObservedStage = 1;

    [Header("Referensi Quest 1 (Trivia / Tour Guide Awal)")]
    [Tooltip("Pemberi quest 1 di awal pulau Pianemo")]
    [SerializeField] private GameObject quest1Giver;

    [Header("Referensi NPC Locked & Unlocked")]
    [Tooltip("NPC saat quest masih terkunci")]
    [SerializeField] private GameObject npcLocked;

    [Tooltip("NPC saat quest sudah terbuka")]
    [SerializeField] private GameObject npcUnlocked;

    [Header("Referensi Quest 2 & 3 (Tangga & Petugas Pengaman)")]
    [Tooltip("Trigger transisi tangga")]
    [SerializeField] private GameObject tanggaTrigger;

    [Tooltip("Petugas Pengaman di ujung tangga")]
    [SerializeField] private GameObject petugasPengaman;

    [Header("Referensi Quest 4 & 5 (Panjat Tebing & Viewpoint Foto)")]
    [Tooltip("Stage aktifnya trigger panjat tebing (default 4, setelah bicara dengan Petugas Pengaman)")]
    [SerializeField] private int tebingClimbStage = 4;

    [Tooltip("Trigger interaksi panjat tebing")]
    [SerializeField] private GameObject tebingClimbTrigger;

    [Tooltip("Zona pengambilan foto di ujung viewpoint")]
    [SerializeField] private GameObject photoTriggerZone;

    [Header("Referensi Quest 6 (Dermaga / Selesai Pulau)")]
    [Tooltip("Trigger kembali ke dermaga untuk berpindah pulau")]
    [SerializeField] private GameObject dermagaExitTrigger;

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
        if (quest1Giver == null) quest1Giver = GameObject.Find("Quest1_Giver");
        if (npcLocked == null) npcLocked = GameObject.Find("NPC_Locked");
        if (npcUnlocked == null) npcUnlocked = GameObject.Find("NPC_Unlocked");
        if (tanggaTrigger == null) tanggaTrigger = GameObject.Find("TanggaFade");
        if (petugasPengaman == null) petugasPengaman = GameObject.Find("QuestGiver2");
        if (tebingClimbTrigger == null) tebingClimbTrigger = GameObject.Find("WallInteractionTrigger");
        if (photoTriggerZone == null) photoTriggerZone = GameObject.Find("QuestTriggerZone");
        if (dermagaExitTrigger == null) dermagaExitTrigger = GameObject.Find("Area_Teleport_Trigger");
    }

    private void Start()
    {
        if (MasterQuestManager.Instance != null)
        {
            MasterQuestManager.Instance.SetActiveIsland(IslandType.Piaynemo);

            if (MasterQuestManager.Instance.CurrentStage < 1)
            {
                MasterQuestManager.Instance.AdvanceStageTo(1);
            }

            Debug.Log($"<color=cyan>[PianemoLandSceneManager] STAGE PIANEMO SAAT INI: {MasterQuestManager.Instance.CurrentStage}</color>");
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

        Debug.Log($"<color=cyan>[PianemoLandSceneManager] Refresh State Pianemo. Stage Aktif: {stage}</color>");

        // Stage 1: Tour guide / trivia di awal pulau
        if (quest1Giver != null)
        {
            quest1Giver.SetActive(stage == 1);
        }

        // Stage 2: Tangga kayu menuju atas
        if (tanggaTrigger != null)
        {
            tanggaTrigger.SetActive(stage >= 1 && stage < 3);
        }

        // Stage 3: Petugas pengaman di puncak tangga
        if (petugasPengaman != null)
        {
            petugasPengaman.SetActive(stage >= 2);
        }

        // Stage 4: Panjat tebing (hanya aktif setelah bicara dengan Petugas Pengaman, nonaktif begitu sampai atas)
        if (tebingClimbTrigger != null)
        {
            tebingClimbTrigger.SetActive(stage == tebingClimbStage);
        }

        // Stage 5: Foto viewpoint
        if (photoTriggerZone != null)
        {
            photoTriggerZone.SetActive(stage >= 4);
        }

        // Stage 6+: Kembali ke dermaga untuk ke pulau berikutnya
        if (dermagaExitTrigger != null)
        {
            dermagaExitTrigger.SetActive(stage >= 5);
        }


        // NPC Locked / Unlocked
        if (npcLocked != null && npcUnlocked != null)
        {
            bool isUnlocked = stage >= 5; // Misal, NPC terbuka setelah stage
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
