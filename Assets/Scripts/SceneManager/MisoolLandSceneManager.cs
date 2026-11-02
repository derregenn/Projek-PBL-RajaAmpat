using UnityEngine;

public class MisoolLandSceneManager : MonoBehaviour
{
    public static MisoolLandSceneManager Instance { get; private set; }
    private int lastObservedStage = -1;

    [Header("Referensi NPC & Objek Darat")]
    [SerializeField] private GameObject penyuInteractive;  // Point 1
    [SerializeField] private GameObject tourGuideStart;     // Point 1 (Muncul setelah Penyu bebas)
    [SerializeField] private GameObject petugasKonservasi; // Point 2, 4, 6
    [SerializeField] private GameObject ibuIbuGroup;        // Point 7 & 9
    [SerializeField] private GameObject tourGuideEnd;      // Point 9 (Di sebelah kanan MC)
    [SerializeField] private Transform tourGuideEndSpot;   // Titik koordinat sebelah kanan MC

    private void Awake()
    {

        // TAMBAHKAN BARIS INI SEMENTARA HANYA UNTUK TESTING EDITOR
        PlayerPrefs.SetInt("Misool_QuestStage", 0);
        PlayerPrefs.Save();
        // ---------------------------------------------------------

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        RefreshLandState();
    }

    private void Update()
    {
        int stage = PlayerPrefs.GetInt("Misool_QuestStage", 0);
        if (stage != lastObservedStage)
            RefreshLandState();
    }

    public void RefreshLandState()
    {
        int stage = PlayerPrefs.GetInt("Misool_QuestStage", 0);
        lastObservedStage = stage;

        // Point 1: Penyu Sisik
        if (penyuInteractive != null)
            penyuInteractive.SetActive(stage == 0);

        // Tour Guide Awal (Aktif dari Stage 1 sampai sebelum Stage 9)
        if (tourGuideStart != null)
            tourGuideStart.SetActive(stage >= 1 && stage < 9);

        // Petugas Konservasi (Aktif dari Stage 1 ke atas)
        if (petugasKonservasi != null)
            petugasKonservasi.SetActive(stage >= 1);

        // Point 7 & 9: Ibu-Ibu Warga hanya aktif pada Stage 6, 7, dan 8
        if (ibuIbuGroup != null)
            ibuIbuGroup.SetActive(stage >= 6 && stage <= 8);

        // Point 9 & 10: Akhir Quest (Tour Guide pindah ke kanan MC)
        if (stage >= 9)
        {
            if (tourGuideStart != null) tourGuideStart.SetActive(false);
            if (ibuIbuGroup != null) ibuIbuGroup.SetActive(false);

            if (tourGuideEnd != null && tourGuideEndSpot != null)
            {
                tourGuideEnd.transform.position = tourGuideEndSpot.position;
                tourGuideEnd.SetActive(true);
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
        if (ibuIbuGroup != null) ibuIbuGroup.SetActive(false);
        if (tourGuideStart != null) tourGuideStart.SetActive(false);

        // 2. Pindahkan & Nyalakan Tour Guide di sebelah kanan Player
        if (tourGuideEnd != null && tourGuideEndSpot != null)
        {
            tourGuideEnd.transform.position = tourGuideEndSpot.position;
            tourGuideEnd.SetActive(true);
        }

        // 3. Simpan Stage 9 (Selesai Total / Free Roam)
        PlayerPrefs.SetInt("Misool_QuestStage", 9);
        PlayerPrefs.Save();

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