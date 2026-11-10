using UnityEngine;

public class SawinggraiLandSceneManager : MonoBehaviour
{
    public static SawinggraiLandSceneManager Instance { get; private set; }
    private int lastObservedStage = 0;

    [Header("Referensi Quest 2 (Warlok Bawah Rumah)")]

    [SerializeField] private GameObject warlok1;
    [SerializeField] private GameObject warlok2Stage2;     // Memberi quest
    [SerializeField] private GameObject warlok2Stage5;     // Lapor selesai dari laut
    [SerializeField] private GameObject warlok2Stage6;     // Pasif (Free Roam)

    [Header("Referensi Quest 3 (Penjual Foto & Burung)")]
    [SerializeField] private GameObject penjualFotoQuest;  // NPC Penjual aktif di Stage 6
    [SerializeField] private GameObject burungCendrawasih; // Target QTE aktif di Stage 6

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (MasterQuestManager.Instance == null) return;
        Debug.Log("<color=cyan>STAGE SAAT INI ADALAH: " + MasterQuestManager.Instance.CurrentStage + "</color>");
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

        // 1. PENGATURAN WARLOK 1
        if (warlok1 != null) warlok1.SetActive(stage == 0 || stage == 1 || stage == 2);

        // 1. PENGATURAN WARLOK 2
        if (warlok2Stage2 != null) warlok2Stage2.SetActive(stage == 0 || stage == 1 || stage == 2);
        if (warlok2Stage5 != null) warlok2Stage5.SetActive(stage == 4 || stage == 5); // Lapor selesai dari laut

        // Aktif terus sebagai NPC pasif setelah quest ikan selesai
        if (warlok2Stage6 != null) warlok2Stage6.SetActive(stage >= 6);


        // 2. PENGATURAN QUEST 3 (PENJUAL FOTO)
        // Menurut rules: saat pemain free roam (stage < 6), penjual foto pasif/mati, cendrawasih mati
        if (penjualFotoQuest != null)
        {
            penjualFotoQuest.SetActive(stage == 6); // Baru bisa diajak bicara saat disuruh Warlok 2
        }

        if (burungCendrawasih != null)
        {
            burungCendrawasih.SetActive(stage >= 0 && stage <= 6); // Cendrawasih aktif saat pemain free roam, mati saat sudah disuruh Warlok 2
        }
    }
}