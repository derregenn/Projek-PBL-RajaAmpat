using UnityEngine;

public class SawinggraiLandSceneManager : MonoBehaviour
{
    public static SawinggraiLandSceneManager Instance { get; private set; }
    private int lastObservedStage = 0;

    [Header("Referensi Quest 2 (Warlok Bawah Rumah)")]

    [SerializeField] private GameObject warlok1;
    [SerializeField] private GameObject pohonObstacle;     // Memberi quest
    [SerializeField] private GameObject warlok2Stage2;     // Memberi quest
    [SerializeField] private GameObject warlok2Stage5;     // Lapor selesai dari laut
    [SerializeField] private GameObject warlok2Stage6;     // Pasif (Free Roam)

    [Header("Referensi Quest 3 (Penjual Foto & Burung)")]
    [SerializeField] private GameObject penjualFotoQuest;  // NPC Penjual aktif di Stage 6
    [SerializeField] private GameObject penjualFotoAfterQuest;  // NPC Penjual aktif di Stage 7 (Setelah Quest Selesai)
    [SerializeField] private GameObject burungCendrawasih; // Target QTE aktif di Stage 6
    [SerializeField] private GameObject[] stepCendrawasih; // Langkah-langkah Cendrawasih, aktif hanya di Stage 6

    [Header("Referensi NPC Locked & Unlocked")]
    [Tooltip("NPC saat quest masih terkunci")]
    [SerializeField] private GameObject npcLocked;

    [Tooltip("NPC saat quest sudah terbuka")]
    [SerializeField] private GameObject npcUnlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (MasterQuestManager.Instance == null) return;
        MasterQuestManager.Instance.SetActiveIsland(IslandType.Sawinggrai);
        MasterQuestManager.Instance.EnsureStageInitialized();
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
        if (pohonObstacle != null) pohonObstacle.SetActive(stage == 0 || stage == 1 || stage == 2);

        // 1. PENGATURAN WARLOK 2
        if (warlok2Stage2 != null) warlok2Stage2.SetActive(stage == 0 || stage == 1 || stage <= 3);
        if (warlok2Stage5 != null) warlok2Stage5.SetActive(stage == 4 || stage == 5); // Lapor selesai dari laut

        // Aktif terus sebagai NPC pasif setelah quest ikan selesai
        if (warlok2Stage6 != null) warlok2Stage6.SetActive(stage >= 6);


        // 2. PENGATURAN QUEST 3 (PENJUAL FOTO)
        // Menurut rules: saat pemain free roam (stage < 6), penjual foto pasif/mati, cendrawasih mati
        if (penjualFotoQuest != null)
        {
            penjualFotoQuest.SetActive(stage == 6); // Baru bisa diajak bicara saat disuruh Warlok 2
        }

        if (penjualFotoAfterQuest != null)
        {
            penjualFotoAfterQuest.SetActive(stage >= 7); // Baru bisa diajak bicara setelah selesai quest
        }

        if (stepCendrawasih != null && stepCendrawasih.Length > 0)
        {
            for (int i = 0; i < stepCendrawasih.Length; i++)
            {
                if (stepCendrawasih[i] != null)
                {
                    stepCendrawasih[i].SetActive(stage == 6); // Semua langkah aktif saat stage 6, mati saat stage lain
                }
            }
        }

        if (burungCendrawasih != null)
        {
            burungCendrawasih.SetActive(stage >= 6 && stage <= 7); // Cendrawasih aktif saat pemain free roam, mati saat sudah disuruh Warlok 2
        }

        // 3. PENGATURAN NPC LOCKED & UNLOCKED (Baru terbuka setelah Quest 3 / Stage 7 selesai)
        if (npcLocked != null && npcUnlocked != null)
        {
            bool isUnlocked = stage >= 7 || QuestJournalManager.CanCompleteIslandOrOpenMap(IslandName.Sawinggrai);
            npcLocked.SetActive(!isUnlocked);
            npcUnlocked.SetActive(isUnlocked);
        }
    }
}