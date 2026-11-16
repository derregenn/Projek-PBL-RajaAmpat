using UnityEngine;

public class WaisaiLandSceneManager : MonoBehaviour
{
    public static WaisaiLandSceneManager Instance { get; private set; }
    private int lastObservedStage = -1;

    [Header("Referensi NPC & Objek Waisai")]
    [Tooltip("Petugas loket informasi / Hub awal")]
    [SerializeField] private GameObject petugasHub;

    [Tooltip("Tour Guide untuk Quest 2 / Trivia Waisai")]
    [SerializeField] private GameObject npcQuest2;

    [Tooltip("Warlok penduduk lokal Waisai")]
    [SerializeField] private GameObject warlok;

    [Tooltip("Dialog / Canvas setelah menyelesaikan Trivia")]
    [SerializeField] private GameObject dialogueAfterTrivia;

    [Tooltip("Trigger atau Teleporter di dermaga menuju pulau berikutnya")]
    [SerializeField] private GameObject dermagaTrigger;

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
        if (petugasHub == null) petugasHub = GameObject.Find("PetugasHub");
        if (npcQuest2 == null) npcQuest2 = GameObject.Find("NPCQuest2");
        if (warlok == null) warlok = GameObject.Find("Warlok");
        if (dialogueAfterTrivia == null) dialogueAfterTrivia = GameObject.Find("DialogueAfterTrivia");
        if (dermagaTrigger == null) dermagaTrigger = GameObject.Find("Dermaga_Waisai");
    }

    private void Start()
    {
        if (MasterQuestManager.Instance != null)
        {
            MasterQuestManager.Instance.SetActiveIsland(IslandType.Waisai);
            Debug.Log($"<color=cyan>[WaisaiLandSceneManager] STAGE WAISAI SAAT INI: {MasterQuestManager.Instance.CurrentStage}</color>");
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

        Debug.Log($"<color=cyan>[WaisaiLandSceneManager] Refresh State Waisai. Stage Aktif: {stage}</color>");

        // Stage 0: Bertemu Petugas Hub di loket
        if (petugasHub != null)
        {
            petugasHub.SetActive(stage == 0 || stage == 1);
        }

        // Stage 1: Bertemu Tour Guide / Menjawab Trivia
        if (npcQuest2 != null)
        {
            npcQuest2.SetActive(stage >= 1);
        }

        // Warlok aktif untuk eksplorasi dan interaksi
        if (warlok != null)
        {
            warlok.SetActive(true);
        }

        // Stage 2+: Selesai Waisai, dermaga terbuka untuk melanjutkan ke pulau berikutnya
        if (dermagaTrigger != null)
        {
            dermagaTrigger.SetActive(stage >= 2);
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
