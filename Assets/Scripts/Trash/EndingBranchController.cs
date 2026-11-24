using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Mengelola percabangan ending di scene Waisai:
/// Menentukan Tour Guide yang aktif berdasarkan progress sampah global:
///   - Sampah < 100% : Aktifkan TourGuide_NormalEnding (Nonaktifkan Alt)
///   - Sampah 100%   : Aktifkan TourGuide_AltEnding (Nonaktifkan Normal)
///
/// Catatan: Sistem dialog dan sertifikat penghargaan sudah diatur langsung 
/// di masing-masing NPC Tour Guide (via Dialogue.cs & onDialogueEnd).
/// </summary>
public class EndingBranchController : MonoBehaviour
{
    public static EndingBranchController Instance { get; private set; }

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
            tourGuideNormal = GameObject.Find("TourGuide_NormalEnding");
        }

        if (tourGuideAlt == null)
        {
            // Cari objek bahkan jika dalam kondisi non-aktif di scene
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject go in allObjects)
            {
                if (go.name == "TourGuide_AltEnding" && go.scene == gameObject.scene)
                {
                    tourGuideAlt = go;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Memeriksa persentase sampah global dan mengaktifkan/menonaktifkan Tour Guide yang sesuai.
    /// </summary>
    public void CheckAndBranchEnding()
    {
        AutoFindTourGuidesIfEmpty();

        bool is100Percent = false;

        if (testForceAltEnding)
        {
            is100Percent = true;
            Debug.Log("[EndingBranchController] Mode Testing aktif: testForceAltEnding = true -> Mengaktifkan Alt Ending.");
        }
        else if (GlobalTrashManager.Instance != null)
        {
            is100Percent = GlobalTrashManager.Instance.IsAllTrashCollected();
            Debug.Log($"[EndingBranchController] Cek Ending. 100% Sampah: {is100Percent} " +
                      $"({GlobalTrashManager.Instance.TotalCollected}/{GlobalTrashManager.Instance.TotalInGame})");
        }
        else
        {
            Debug.LogWarning("[EndingBranchController] GlobalTrashManager.Instance tidak ditemukan (mungkin play langsung dari scene ini). Default ke Normal Ending.");
            is100Percent = false;
        }

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
