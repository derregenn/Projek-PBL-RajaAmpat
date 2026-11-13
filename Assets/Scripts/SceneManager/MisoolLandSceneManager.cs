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
    [SerializeField] private GameObject ibuIbuStage8;        // Point 9 (Ibu-Ibu Warga)
    [SerializeField] private GameObject tourGuideEnd;      // Point 9 (Di sebelah kanan MC)
    [SerializeField] private Transform tourGuideEndSpot;   // Titik koordinat sebelah kanan MC

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (MasterQuestManager.Instance == null)
        {
            Debug.LogWarning("[MisoolLandSceneManager] MasterQuestManager tidak ditemukan; menunggu manager tersedia.");
            return;
        }

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
        if (stage < 9 && QuestJournalManager.IsIslandCompletedPersistent(IslandName.Misool))
        {
            stage = 9;
        }
        lastObservedStage = stage;

        // Aktifkan NPC yang sesuai dengan stage saat ini
        if (petugasStage1 != null) petugasStage1.SetActive(stage == 1 || stage == 2);
        if (petugasStage3 != null) petugasStage3.SetActive(stage == 3);
        if (petugasStage5 != null) petugasStage5.SetActive(stage == 5);

        // Point 1: Penyu Sisik
        if (penyuInteractive != null)
            penyuInteractive.SetActive(stage == 0 || stage == 1);

        // Tour Guide Awal (Aktif dari Stage 1 sampai sebelum Stage 9)
        if (tourGuideStart != null)
            tourGuideStart.SetActive(stage >= 1 && stage < 9);

        // Point 7 & 9: Ibu-Ibu Warga hanya aktif pada Stage 6, 7, dan 8
        if (ibuIbuStage6 != null)
            ibuIbuStage6.SetActive(stage == 6 || stage == 7);
        if (ibuIbuStage8 != null)
            ibuIbuStage8.SetActive(stage == 8);

        // Point 9 & 10: Akhir Quest (Tour Guide pindah ke kanan MC)
        if (stage >= 9)
        {
            if (tourGuideStart != null) tourGuideStart.SetActive(false);
            if (ibuIbuStage6 != null) ibuIbuStage6.SetActive(false);
            if (ibuIbuStage8 != null) ibuIbuStage8.SetActive(false);

            if (tourGuideEnd != null)
            {
                if (tourGuideEndSpot != null)
                {
                    tourGuideEnd.transform.position = tourGuideEndSpot.position;
                }
                tourGuideEnd.SetActive(true);
                Dialogue endDialogue = tourGuideEnd.GetComponentInChildren<Dialogue>(true);
                endDialogue?.RefreshPostQuestLoopState();
            }
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
        // 1. Hilangkan Ibu-Ibu & Tour Guide Awal
        if (ibuIbuStage6 != null) ibuIbuStage6.SetActive(false);
        if (ibuIbuStage8 != null) ibuIbuStage8.SetActive(false);
        if (tourGuideStart != null) tourGuideStart.SetActive(false);

        // 2. Pindahkan & Nyalakan Tour Guide di sebelah kanan Player
        if (tourGuideEnd != null && tourGuideEndSpot != null)
        {
            tourGuideEnd.transform.position = tourGuideEndSpot.position;
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