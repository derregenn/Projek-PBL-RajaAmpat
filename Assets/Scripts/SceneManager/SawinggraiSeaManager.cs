using UnityEngine;

public class SawinggraiSeaManager : MonoBehaviour
{
    public static SawinggraiSeaManager Instance { get; private set; }
    private int lastObservedStage = -1;

    [Header("Referensi Interaksi Quest 2")]
    [Tooltip("Titik interaksi (Hold E) untuk menebar umpan")]
    [SerializeField] private GameObject fishFeedTrigger;

    [Tooltip("Titik interaksi (Tombol F) untuk memotret ikan")]
    [SerializeField] private GameObject fishPhotoTrigger;

    [Header("Referensi Transisi (Teleporter)")]
    [Tooltip("Trigger untuk kembali ke darat (Akan dimatikan selama quest belum selesai)")]
    [SerializeField] private GameObject returnToLandTeleporter;

    [Header("Referensi Visual (Opsional)")]
    [Tooltip("Objek visual kawanan ikan (selalu aktif, tapi state animasinya diatur terpisah)")]
    [SerializeField] private GameObject kawananIkan;

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
            Debug.LogWarning("[SawinggraiSeaManager] MasterQuestManager tidak ditemukan di scene ini!");
            return;
        }

        RefreshSeaState();
    }

    private void Update()
    {
        if (MasterQuestManager.Instance == null) return;

        int stage = MasterQuestManager.Instance.CurrentStage;
        // Otomatis memperbarui tampilan laut jika stage berubah (misal setelah tebar umpan/foto)
        if (stage != lastObservedStage)
        {
            RefreshSeaState();
        }
    }

    public void RefreshSeaState()
    {
        if (MasterQuestManager.Instance == null) return;

        int stage = MasterQuestManager.Instance.CurrentStage;
        lastObservedStage = stage;

        Debug.Log($"<color=cyan>[SawinggraiSeaManager] Refresh State Laut. Stage Aktif: {stage}</color>");

        // 1. LOGIKA INTERAKSI TEBAR UMPAN
        // Hanya aktif di Stage 3 (Pemain baru saja disuruh Warlok 2 untuk menyelam)
        if (fishFeedTrigger != null)
        {
            fishFeedTrigger.SetActive(stage == 0 || stage == 3);
        }

        // 2. LOGIKA INTERAKSI FOTO IKAN
        // Hanya aktif di Stage 4 (Setelah umpan ditebar, ikan berkumpul)
        if (fishPhotoTrigger != null)
        {
            fishPhotoTrigger.SetActive(stage == 1 || stage == 4);
        }

        // 3. LOGIKA TELEPORTER KEMBALI KE DARAT (RULE: TIDAK BISA KEMBALI SEBELUM FOTO)
        if (returnToLandTeleporter != null)
        {
            // Teleporter dimatikan HANYA jika pemain sedang berada di tengah-tengah Quest 2 (Stage 3 dan 4).
            // Jika sudah foto (Stage >= 5) ATAU jika masuk ke laut di luar quest (Stage < 3), pemain bebas naik.
            bool canReturnToLand = (stage < 3 || stage >= 5);
            returnToLandTeleporter.SetActive(canReturnToLand);
        }

        // 4. VISUAL KAWANAN IKAN
        // Tetap terlihat kapan pun menyelam (Free Roam), tapi tidak bisa diinteraksi jika stage > 4
        if (kawananIkan != null)
        {
            kawananIkan.SetActive(true);
        }
    }
}