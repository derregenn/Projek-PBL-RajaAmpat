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
        RefreshMenuUnlockStates();
    }

    public static bool IsFreeRoamUnlocked()
    {
        return PlayerPrefs.GetInt("NormalEndingReached", 0) == 1 ||
               PlayerPrefs.GetInt("AltEndingReached", 0) == 1 ||
               PlayerPrefs.GetInt("GameCompleted", 0) == 1;
    }

    public static bool IsMainMenuJournalUnlocked()
    {
        return IsFreeRoamUnlocked() || QuestJournalManager.IsPrerequisiteIslandCompleted(IslandName.Waisai);
    }

    public static void ApplyButtonUnlockVisual(UnityEngine.UI.Button button, bool isUnlocked)
    {
        if (button == null) return;

        if (!button.gameObject.activeSelf)
            button.gameObject.SetActive(true);

        button.interactable = isUnlocked;

        // Avoid double-multiplying disabledColor alpha with Image.color alpha.
        UnityEngine.UI.ColorBlock cb = button.colors;
        cb.disabledColor = Color.white;
        button.colors = cb;

        UnityEngine.UI.Image img = button.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
        {
            img.color = isUnlocked ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.5f);
        }
    }

    public void RefreshMenuUnlockStates()
    {
        ApplyButtonUnlockVisual(btnFreeRoam, IsFreeRoamUnlocked());
        ApplyButtonUnlockVisual(btnTravelJournal, IsMainMenuJournalUnlocked());
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
        if (!IsFreeRoamUnlocked()) return;

        GlobalTrashManager.Instance?.ActivateFreeRoamMode();

        string targetHub = string.IsNullOrEmpty(freeRoamStartScene) ? "02_Waisai" : freeRoamStartScene;
        PlayerPrefs.SetInt("GTM_FreeRoamMode", 1);
        PlayerPrefs.SetString("SavedScene", targetHub);
        PlayerPrefs.SetInt("ShouldRestoreSavedPosition", 0);
        PlayerPrefs.DeleteKey("PlayerX");
        PlayerPrefs.DeleteKey("PlayerY");
        PlayerPrefs.DeleteKey("PlayerZ");
        PlayerPrefs.Save();

        Time.timeScale = 1f;
        // Muat ke Waisai sebagai hub
        SceneManager.LoadScene(targetHub);
        Debug.Log("[FreeRoamManager] Free Roam dimulai.");
    }

    private float lastJournalToggleTime = -10f;

    /// <summary>
    /// Dipanggil oleh tombol Travel Journal di Main Menu setelah Pulau Waisai selesai.
    /// </summary>
    public void OpenTravelJournal()
    {
        if (!IsMainMenuJournalUnlocked()) return;
        if (Time.unscaledTime - lastJournalToggleTime < 0.08f) return;
        lastJournalToggleTime = Time.unscaledTime;

        if (QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.UnlockJournal();
            QuestJournalManager.Instance.ToggleJournalPanel();
            return;
        }

        QuestTabUI tabUI = FindFirstObjectByType<QuestTabUI>(FindObjectsInactive.Include);
        if (tabUI != null)
        {
            bool willOpen = !tabUI.gameObject.activeSelf;
            tabUI.EnsureStandardTransform();
            tabUI.EnsureCanvasSortingOrder();
            if (willOpen) tabUI.transform.SetAsLastSibling();
            tabUI.gameObject.SetActive(willOpen);
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
