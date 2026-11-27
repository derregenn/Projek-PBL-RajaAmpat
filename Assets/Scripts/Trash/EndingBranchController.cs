using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Mengelola percabangan ending di scene Waisai Ending dan Cutscene Airstrip Ending:
/// Menentukan Tour Guide yang aktif berdasarkan progress sampah global:
///   - Sampah < 100% : Aktifkan TourGuide_NormalEnding (Nonaktifkan Alt)
///   - Sampah 100%   : Aktifkan TourGuide_AltEnding (Nonaktifkan Normal)
///
/// Catatan:
/// - Sistem dialog dan sertifikat penghargaan diatur langsung di masing-masing NPC Tour Guide.
/// - Status ending dari 11_Waisai Ending otomatis tersinkronisasi ke 12_CutsceneAirstripEnding
///   (baik melalui GlobalTrashManager maupun status sesi jika sedang mode testing).
/// </summary>
public class EndingBranchController : MonoBehaviour
{
    public static EndingBranchController Instance { get; private set; }

    // Menyimpan hasil pengecekan dari scene Waisai Ending agar tetap konsisten saat lanjut ke Airstrip Ending
    // (terutama saat pengujian langsung di Editor dengan testForceAltEnding).
    private static bool? sessionAltEndingOverride;
    private static bool isSceneHookRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        sessionAltEndingOverride = null;
        if (isSceneHookRegistered)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            isSceneHookRegistered = false;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneHook()
    {
        if (!isSceneHookRegistered)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            isSceneHookRegistered = true;
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Jika di scene tujuan (misal 12_CutsceneAirstripEnding) belum dipasang EndingBranchController,
        // tetap sinkronkan TourGuide_NormalEnding & TourGuide_AltEnding secara otomatis.
        if (scene.name == "12_CutsceneAirstripEnding" && FindFirstObjectByType<EndingBranchController>() == null)
        {
            bool is100Percent = ResolveIsAltEnding(false);
            GameObject normalGuide = FindInSceneByName(scene, "TourGuide_NormalEnding");
            GameObject altGuide = FindInSceneByName(scene, "TourGuide_AltEnding");

            if (normalGuide != null || altGuide != null)
            {
                if (normalGuide != null) normalGuide.SetActive(!is100Percent);
                if (altGuide != null) altGuide.SetActive(is100Percent);
                Debug.Log($"[EndingBranchController] Auto-sync scene '{scene.name}' -> AltEnding: {is100Percent}");
            }
        }
    }

    [Header("Referensi Tour Guide (Check & Uncheck)")]
    [Tooltip("GameObject Tour Guide untuk Normal Ending (sampah belum 100%)")]
    public GameObject tourGuideNormal;

    [Tooltip("GameObject Tour Guide untuk Alternative Ending (sampah 100%)")]
    public GameObject tourGuideAlt;

    [Header("Opsi Otomatisasi")]
    [Tooltip("Jika dicentang, otomatis cek sampah dan aktifkan Tour Guide yang sesuai saat scene dimulai.")]
    public bool autoCheckOnStart = true;

    [Tooltip("Jika dicentang, otomatis mulai dialog pada Tour Guide yang aktif setelah dicek.")]
    public bool autoStartDialogueOnCheck = false;

    [Header("Mode Testing (Editor Only)")]
    [Tooltip("Centang ini untuk menguji Alternative Ending secara langsung di Unity Editor tanpa harus mengumpulkan sampah.")]
    public bool testForceAltEnding = false;

    [Header("Events Tambahan (Opsional)")]
    public UnityEvent onNormalEnding;
    public UnityEvent onAlternativeEnding;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        AutoFindTourGuidesIfEmpty();
    }

    private void Start()
    {
        if (autoCheckOnStart)
        {
            CheckAndBranchEnding();
        }
    }

    /// <summary>
    /// Mencari referensi Tour Guide secara otomatis jika belum diisi di Inspector.
    /// </summary>
    private void AutoFindTourGuidesIfEmpty()
    {
        if (tourGuideNormal == null)
        {
            tourGuideNormal = FindInSceneByName(gameObject.scene, "TourGuide_NormalEnding");
        }

        if (tourGuideAlt == null)
        {
            tourGuideAlt = FindInSceneByName(gameObject.scene, "TourGuide_AltEnding");
        }
    }

    private static GameObject FindInSceneByName(Scene scene, string targetName)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in allObjects)
        {
            if (go.name == targetName && go.scene == scene)
            {
                return go;
            }
        }
        return null;
    }

    private static bool ResolveIsAltEnding(bool forceAlt)
    {
        if (forceAlt)
        {
            sessionAltEndingOverride = true;
            Debug.Log("[EndingBranchController] Mode Testing aktif: testForceAltEnding = true -> Mengaktifkan Alt Ending.");
            return true;
        }

        if (GlobalTrashManager.Instance != null)
        {
            bool isAllCollected = GlobalTrashManager.Instance.IsAllTrashCollected();
            sessionAltEndingOverride = isAllCollected;
            Debug.Log($"[EndingBranchController] Cek Ending. 100% Sampah: {isAllCollected} " +
                      $"({GlobalTrashManager.Instance.TotalCollected}/{GlobalTrashManager.Instance.TotalInGame})");
            return isAllCollected;
        }

        if (sessionAltEndingOverride.HasValue)
        {
            Debug.Log($"[EndingBranchController] Menggunakan status ending dari scene sebelumnya: AltEnding = {sessionAltEndingOverride.Value}");
            return sessionAltEndingOverride.Value;
        }

        Debug.LogWarning("[EndingBranchController] GlobalTrashManager.Instance tidak ditemukan (mungkin play langsung dari scene ini). Default ke Normal Ending.");
        return false;
    }

    /// <summary>
    /// Memeriksa persentase sampah global dan mengaktifkan/menonaktifkan Tour Guide yang sesuai.
    /// </summary>
    public void CheckAndBranchEnding()
    {
        AutoFindTourGuidesIfEmpty();

        bool is100Percent = ResolveIsAltEnding(testForceAltEnding);

        // Terapkan Check & Uncheck (Active / Inactive) pada GameObject Tour Guide
        if (tourGuideNormal != null)
        {
            tourGuideNormal.SetActive(!is100Percent);
            Debug.Log($"[EndingBranchController] TourGuide_NormalEnding set Active: {!is100Percent}");
        }

        if (tourGuideAlt != null)
        {
            tourGuideAlt.SetActive(is100Percent);
            Debug.Log($"[EndingBranchController] TourGuide_AltEnding set Active: {is100Percent}");
        }

        // Panggil UnityEvent opsional jika ada
        if (is100Percent)
        {
            onAlternativeEnding?.Invoke();
        }
        else
        {
            onNormalEnding?.Invoke();
        }

        // Jika diatur untuk langsung memulai dialog secara otomatis
        if (autoStartDialogueOnCheck)
        {
            GameObject targetGuide = is100Percent ? tourGuideAlt : tourGuideNormal;
            if (targetGuide != null)
            {
                Dialogue dialogue = targetGuide.GetComponent<Dialogue>();
                if (dialogue != null)
                {
                    dialogue.StartDialogue();
                }
            }
        }
    }
}
