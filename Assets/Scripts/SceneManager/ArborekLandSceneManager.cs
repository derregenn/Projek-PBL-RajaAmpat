using UnityEngine;

public class ArborekLandSceneManager : MonoBehaviour
{
    public static ArborekLandSceneManager Instance { get; private set; }
    private int lastObservedStage = -1;

    [Header("Referensi Tetua Adat Berdasarkan Stage")]
    [Tooltip("Tetua Adat di awal (Quest 1 - Kotak Ikan)")]
    [SerializeField] private GameObject tetuaAdatAwal;

    [Tooltip("Tetua Adat saat penyerahan kerang (Quest 2)")]
    [SerializeField] private GameObject tetuaAdatTerimaUmpan;

    [Tooltip("Tetua Adat sebelum prosesi Sasi (Quest 3)")]
    [SerializeField] private GameObject tetuaAdatSasiBefore;

    [Tooltip("Tetua Adat setelah prosesi Sasi (Quest 3/4)")]
    [SerializeField] private GameObject tetuaAdatSasiAfter;

    [Tooltip("Tetua Adat untuk sesi fotografi adat Sasi (Quest 5)")]
    [SerializeField] private GameObject tetuaAdatFotografi;

    [Tooltip("Tetua Adat akhir / terima karang (Quest 6)")]
    [SerializeField] private GameObject tetuaAdatTerimaKarang;

    [Header("Referensi Warga Arborek")]
    [SerializeField] private GameObject npcArborek1;
    [SerializeField] private GameObject npcArborek2;
    [SerializeField] private GameObject npcArborek3;

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
        if (tetuaAdatAwal == null) tetuaAdatAwal = GameObject.Find("NPC_TetuaAdat_Awal");
        if (tetuaAdatTerimaKarang == null) tetuaAdatTerimaKarang = GameObject.Find("NPC_TetuaAdat_TerimaKarang");
        if (tetuaAdatSasiBefore == null) tetuaAdatSasiBefore = GameObject.Find("NPC_TetuaAdat_Sasi_Before");
        if (tetuaAdatSasiAfter == null) tetuaAdatSasiAfter = GameObject.Find("NPC_TetuaAdat_Sasi_After");
        if (tetuaAdatFotografi == null) tetuaAdatFotografi = GameObject.Find("NPC_TetuaAdat_Fotografi");
        if (tetuaAdatTerimaUmpan == null) tetuaAdatTerimaUmpan = GameObject.Find("NPC_TetuaAdat_TerimaUmpan");

        if (npcArborek1 == null) npcArborek1 = GameObject.Find("NPC_Arborek");
        if (npcArborek2 == null) npcArborek2 = GameObject.Find("NPC_Arborek 2  ");
        if (npcArborek3 == null) npcArborek3 = GameObject.Find("NPC_Arborek 3");
    }

    private void Start()
    {
        if (MasterQuestManager.Instance != null)
        {
            MasterQuestManager.Instance.SetActiveIsland(IslandType.Arborek);
            MasterQuestManager.Instance.EnsureStageInitialized();
            Debug.Log($"<color=cyan>[ArborekLandSceneManager] STAGE ARBOREK SAAT INI: {MasterQuestManager.Instance.CurrentStage}</color>");
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

        Debug.Log($"<color=cyan>[ArborekLandSceneManager] Refresh State Arborek. Stage Aktif: {stage}</color>");

        // Stage 0 - 1: Cari dan serahkan kotak umpan.
        if (tetuaAdatAwal != null)
        {
            tetuaAdatAwal.SetActive(stage == 0 || stage == 1);
        }

        // Stage 2: Cari dan serahkan Kerang Terompet; Tetua berpindah ke ujung pantai.
        if (tetuaAdatTerimaKarang != null)
        {
            tetuaAdatTerimaKarang.SetActive(stage == 2);
        }

        // Stage 3 - 5: Tetua tetap di lokasi Quest Sasi selama pemasangan, trivia, dan foto.
        if (tetuaAdatSasiBefore != null)
        {
            tetuaAdatSasiBefore.SetActive(stage >= 3 && stage <= 5);
        }

        // Pertahankan satu objek Tetua di lokasi yang sama sepanjang Quest 2 dan 3.
        if (tetuaAdatSasiAfter != null)
        {
            tetuaAdatSasiAfter.SetActive(false);
        }

        // Tetua yang sama tetap aktif saat objective foto.
        if (tetuaAdatFotografi != null)
        {
            tetuaAdatFotografi.SetActive(false);
        }

        // Stage 6+: Penutupan setelah rangkaian quest selesai.
        if (tetuaAdatTerimaUmpan != null)
        {
            tetuaAdatTerimaUmpan.SetActive(stage >= 6);
        }

        // Warga Arborek tetap aktif sebagai ambient NPC
        if (npcArborek1 != null) npcArborek1.SetActive(true);
        if (npcArborek2 != null) npcArborek2.SetActive(true);
        if (npcArborek3 != null) npcArborek3.SetActive(true);

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
