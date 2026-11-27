using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controller untuk mode Post-Completion (Free Roam) setelah game selesai.
///
/// Tanggung jawab:
///   - Mendeteksi status Free Roam dari PlayerPrefs.
///   - Menyembunyikan objek quest dan sampah yang belum pernah diambil (sudah selesai).
///   - Memastikan NPC dialog loop tetap aktif.
///   - Menyediakan tombol Free Roam di Main Menu.
///
/// CARA SETUP:
///   1. Pasang script ini ke satu GameObject "FreeRoamManager" di Main Menu scene.
///   2. Hubungkan btnFreeRoam ke tombol Free Roam di UI Main Menu.
///   3. FreeRoamManager akan otomatis aktif/nonaktif sesuai status game.
/// </summary>
public class FreeRoamManager : MonoBehaviour
{
    public static FreeRoamManager Instance { get; private set; }

    [Header("UI Main Menu")]
    [Tooltip("Tombol Free Roam di Main Menu (akan diaktifkan jika game sudah tamat).")]
    public UnityEngine.UI.Button btnFreeRoam;

    [Tooltip("Scene yang dimuat saat Free Roam dimulai (Waisai sebagai hub).")]
    [SerializeField] private string freeRoamStartScene = "02_Waisai";

    [Header("Travel Journal Button (Main Menu)")]
    [Tooltip("Tombol Travel Journal di Main Menu (akan diaktifkan jika game sudah tamat).")]
    public UnityEngine.UI.Button btnTravelJournal;

    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        bool gameCompleted = PlayerPrefs.GetInt("GameCompleted", 0) == 1;

        if (btnFreeRoam != null)
        {
            btnFreeRoam.gameObject.SetActive(gameCompleted);
            btnFreeRoam.interactable = gameCompleted;
        }

        if (btnTravelJournal != null)
        {
            btnTravelJournal.gameObject.SetActive(gameCompleted);
            btnTravelJournal.interactable = gameCompleted;
        }
    }

    // =========================================================
    // PUBLIC API — dipanggil dari tombol di Inspector
    // =========================================================

    /// <summary>
    /// Dipanggil oleh tombol Free Roam di Main Menu.
    /// Mengaktifkan mode Free Roam lalu memuat scene hub.
    /// </summary>
    public void StartFreeRoam()
    {
        GlobalTrashManager.Instance?.ActivateFreeRoamMode();

        // Muat ke Waisai sebagai hub
        SceneManager.LoadScene(freeRoamStartScene);
        Debug.Log("[FreeRoamManager] Free Roam dimulai.");
    }

    /// <summary>
    /// Dipanggil oleh tombol Travel Journal di Main Menu setelah game tamat.
    /// </summary>
    public void OpenTravelJournal()
    {
        if (PlayerPrefs.GetInt("GameCompleted", 0) != 1) return;

        if (QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.UnlockJournal();
            QuestJournalManager.Instance.ToggleJournalPanel();
            return;
        }

        QuestTabUI tabUI = FindFirstObjectByType<QuestTabUI>(FindObjectsInactive.Include);
        if (tabUI != null)
        {
            tabUI.gameObject.SetActive(!tabUI.gameObject.activeSelf);
        }
    }

    /// <summary>
    /// Nonaktifkan Free Roam (kembali ke menu utama / normal).
    /// </summary>
    public void ExitFreeRoam()
    {
        GlobalTrashManager.Instance?.DeactivateFreeRoamMode();
        SceneManager.LoadScene("01_MainMenu");
    }
}
