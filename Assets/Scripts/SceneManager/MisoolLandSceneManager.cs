using UnityEngine;

public class MisoolLandSceneManager : MonoBehaviour
{
    public static MisoolLandSceneManager Instance { get; private set; }
    private int lastObservedStage = -1;

    [Header("Referensi NPC & Objek Darat")]
    [SerializeField] private GameObject penyuInteractive;  // Point 1
    [SerializeField] private GameObject tourGuideStart;     // Point 1 (Muncul setelah Penyu bebas)
    [SerializeField] private GameObject petugasStage1;
    [SerializeField] private GameObject petugasStage3;
    [SerializeField] private GameObject petugasStage5;
    [SerializeField] private GameObject ibuIbuStage6;        // Point 7 (Ibu-Ibu Warga)
    [SerializeField] private GameObject ibuIbuStage8;        // Point 9 & Free Roam (Menyala seterusnya >= 8)
    [SerializeField] private GameObject tourGuideEnd;      // Point 9 (Di sebelah kanan MC)

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

        // Pastikan tourGuideEnd selalu mati di awal sebelum stage dibaca
        if (tourGuideEnd != null)
            tourGuideEnd.SetActive(false);
    }

    private void Start()
    {
        if (MasterQuestManager.Instance == null)
        {
            Debug.LogWarning("[MisoolLandSceneManager] MasterQuestManager tidak ditemukan; menunggu manager tersedia.");
            return;
        }

        MasterQuestManager.Instance.SetActiveIsland(IslandType.Misool);
        MasterQuestManager.Instance.EnsureStageInitialized();
        Debug.Log("<color=cyan>STAGE SAAT INI ADALAH: " + MasterQuestManager.Instance.CurrentStage + "</color>");
        RefreshLandState();
    }

    private void Update()
    {
        if (MasterQuestManager.Instance == null)
            return;

        int stage = MasterQuestManager.Instance.CurrentStage;
        if (stage != lastObservedStage)
            RefreshLandState();
    }

    public void RefreshLandState()
    {
        if (MasterQuestManager.Instance == null)
            return;

        int stage = MasterQuestManager.Instance.CurrentStage;
        lastObservedStage = stage;

        if (stage == 5)
        {
            MisoolQuestManager.ShowReturnToOfficerPrompt();
        }

        // Aktifkan NPC yang sesuai dengan stage saat ini
        if (petugasStage1 != null) petugasStage1.SetActive(stage == 1 || stage == 2);
        if (petugasStage3 != null) petugasStage3.SetActive(stage == 3 || stage == 4);
        if (petugasStage5 != null) petugasStage5.SetActive(stage >= 5);

        // Point 1: Penyu Sisik
        if (penyuInteractive != null)
            penyuInteractive.SetActive(stage == 0 || stage == 1);

        // Tour Guide Awal (Aktif dari Stage 1 sampai Stage 7)
        if (tourGuideStart != null)
            tourGuideStart.SetActive(stage >= 1 && stage < 8);

        // Point 7 & 9: Ibu-Ibu Warga Stage 6-7, lalu Ibu-Ibu Stage 8 menyala seterusnya (>= 8)
        if (ibuIbuStage6 != null)
            ibuIbuStage6.SetActive(stage == 6 || stage == 7);
        if (ibuIbuStage8 != null)
            ibuIbuStage8.SetActive(stage >= 8);

        // Point 9 & 10: Akhir Quest (Tour Guide pindah ke kanan MC)
        bool isPoint9 = stage >= 9;

        if (tourGuideEnd != null)
            tourGuideEnd.SetActive(isPoint9);

        if (isPoint9)
        {
            if (tourGuideStart != null) tourGuideStart.SetActive(false);
            if (ibuIbuStage6 != null) ibuIbuStage6.SetActive(false);
        }

        // NPC Locked & Unlocked
        if (npcLocked != null && npcUnlocked != null)
        {
            bool isUnlocked = stage >= 9;
            npcLocked.SetActive(!isUnlocked);
            npcUnlocked.SetActive(isUnlocked);
        }
    }

    // Dipanggil saat Event OnDialogueEnd() Ibu-Ibu di Point 9 Selesai
    public void TriggerPoint9FadeAndTourGuide()
    {
        if (UniversalFader.Instance != null)
        {
            UniversalFader.Instance.FadeTransition(() =>
            {
                ExecutePoint9Transition();
            });
        }
        else
        {
            Debug.LogWarning("[MisoolLandSceneManager] UniversalFader tidak ditemukan, memproses transisi langsung.");
            ExecutePoint9Transition();
        }
    }

    private void ExecutePoint9Transition()
    {
        // 1. Hilangkan Ibu-Ibu Stage 6 & Tour Guide Awal (ibuIbuStage8 tetap dibiarkan menyala)
        if (ibuIbuStage6 != null) ibuIbuStage6.SetActive(false);
        if (tourGuideStart != null) tourGuideStart.SetActive(false);

        // 2. Nyalakan Tour Guide di sebelah kanan Player
        if (tourGuideEnd != null)
        {
            tourGuideEnd.SetActive(true);
        }

        // 3. Simpan Stage 9 (Selesai Total / Free Roam)
        MasterQuestManager.Instance.AdvanceStageTo(9);

        // 4. Jalankan Dialog Penutup Tour Guide (Point 9)
        if (tourGuideEnd != null)
        {
            Dialogue tgDialogue = tourGuideEnd.GetComponent<Dialogue>();
            if (tgDialogue != null)
            {
                tgDialogue.StartDialogue();
            }
        }
    }
}