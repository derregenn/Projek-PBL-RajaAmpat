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

            if (MasterQuestManager.Instance.CurrentStage < 1)
            {
                MasterQuestManager.Instance.AdvanceStageTo(1);
            }

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
        if (ArborekFaderManager.IsAnyTransitioning) return;

        int stage = MasterQuestManager.Instance.CurrentStage;
        lastObservedStage = stage;

        Debug.Log($"<color=cyan>[ArborekLandSceneManager] Refresh State Arborek. Stage Aktif: {stage}</color>");

        // Stage 0 - 1: Cari dan serahkan kotak umpan.
        if (tetuaAdatAwal != null)
        {
            tetuaAdatAwal.SetActive(stage == 0 || stage == 1);
        }

        // Stage 2: Cari dan serahkan Kerang Terompet; Tetua menunggu di posisi awal/terima karang.
        if (tetuaAdatTerimaKarang != null)
        {
            tetuaAdatTerimaKarang.SetActive(stage == 2);
        }

        // Stage 3: Ritual Sasi Before & pemasangan Sasi.
        if (tetuaAdatSasiBefore != null)
        {
            tetuaAdatSasiBefore.SetActive(stage == 3);
        }

        // Stage 4: Sasi After selesai dipasang, bicara dengan Tetua Adat.
        if (tetuaAdatSasiAfter != null)
        {
            tetuaAdatSasiAfter.SetActive(stage == 4);
        }

        // Stage 5: Tetua Adat Fotografi & Trivia. Stage 6: Tetua tetap terlihat pasif bersama Tour Guide.
        if (tetuaAdatFotografi != null)
        {
            bool showFotoTetua = (stage == 5 || stage == 6);
            tetuaAdatFotografi.SetActive(showFotoTetua);
            if (stage == 6)
            {
                Collider2D col = tetuaAdatFotografi.GetComponent<Collider2D>();
                if (col != null) col.enabled = false;
                Dialogue dlg = tetuaAdatFotografi.GetComponentInChildren<Dialogue>();
                if (dlg != null) dlg.enabled = false;
            }
        }

        // TerimaUmpan hanya opsional bila tidak aktif di akhir (jangan nyalakan lagi di stage >= 6).
        if (tetuaAdatTerimaUmpan != null && stage >= 6)
        {
            tetuaAdatTerimaUmpan.SetActive(false);
        }

        // Warga Arborek tetap aktif sebagai ambient NPC
        if (npcArborek1 != null) npcArborek1.SetActive(true);
        if (npcArborek2 != null) npcArborek2.SetActive(true);
        if (npcArborek3 != null) npcArborek3.SetActive(true);

        // NPC Locked / Unlocked (terbuka setelah foto & transisi akhir di Stage 7+)
        if (npcLocked != null && npcUnlocked != null)
        {
            bool isUnlocked = stage >= 7;
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
