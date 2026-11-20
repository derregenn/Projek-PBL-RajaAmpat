using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Mengelola percabangan ending di scene Waisai:
///   - Normal Ending   : sampah belum 100%
///   - Alternative Ending (100%) : semua sampah terkumpul → tampilkan Sertifikat Pop-Up
///
/// CARA SETUP DI UNITY INSPECTOR:
///   1. Buat GameObject "EndingBranchController" di scene Waisai.
///   2. Pasang script ini.
///   3. Hubungkan referensi UI: sertifikat panel, tombol, teks.
///   4. Isi UnityEvent untuk dialog ending Normal dan Alt.
///   5. Panggil CheckAndBranchEnding() dari On Dialogue End Tour Guide Waisai.
/// </summary>
public class EndingBranchController : MonoBehaviour
{
    // =========================================================
    // SINGLETON (satu per scene)
    // =========================================================
    public static EndingBranchController Instance { get; private set; }

    // =========================================================
    // INSPECTOR FIELDS
    // =========================================================

    [Header("Panel Sertifikat (Alternative Ending)")]
    [Tooltip("GameObject panel sertifikat penghargaan 100%.")]
    public GameObject certificatePanel;
    [Tooltip("Tombol tutup sertifikat.")]
    public Button btnCloseCertificate;
    [Tooltip("Teks nama / detail sertifikat (opsional).")]
    public TextMeshProUGUI certificateBodyText;

    [Header("Durasi Pop-up Sertifikat")]
    [SerializeField] private float certificatePopupDuration = 0.4f;

    [Header("Events: Normal Ending")]
    [Tooltip("Dialog / scene load untuk Normal Ending (sampah < 100%).")]
    public UnityEvent onNormalEnding;

    [Header("Events: Alternative Ending (100%)")]
    [Tooltip("Dipanggil tepat SETELAH sertifikat ditutup.")]
    public UnityEvent onAlternativeEndingAfterCertificate;

    [Header("Events: Setelah Sertifikat Ditutup")]
    [Tooltip("Selalu dipanggil setelah sertifikat ditutup, apapun ending-nya.")]
    public UnityEvent onCertificateClosed;

    // =========================================================
    // STATE
    // =========================================================

    private bool isAltEnding;
    private CanvasGroup certCanvasGroup;
    private RectTransform certRect;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (certificatePanel != null)
        {
            certificatePanel.SetActive(false);
            certCanvasGroup = certificatePanel.GetComponent<CanvasGroup>();
            if (certCanvasGroup == null) certCanvasGroup = certificatePanel.AddComponent<CanvasGroup>();
            certRect = certificatePanel.GetComponent<RectTransform>();
        }

        if (btnCloseCertificate != null)
        {
            btnCloseCertificate.onClick.RemoveListener(CloseCertificate);
            btnCloseCertificate.onClick.AddListener(CloseCertificate);
        }
    }

    // =========================================================
    // MAIN API
    // =========================================================

    /// <summary>
    /// Panggil dari On Dialogue End Tour Guide di Waisai.
    /// Memeriksa progress sampah, lalu menentukan jalur ending.
    /// </summary>
    public void CheckAndBranchEnding()
    {
        bool all100 = GlobalTrashManager.Instance != null && GlobalTrashManager.Instance.IsAllTrashCollected();

        Debug.Log($"[EndingBranchController] Cek Ending. 100% Sampah: {all100} " +
                  $"({GlobalTrashManager.Instance?.TotalCollected}/{GlobalTrashManager.Instance?.TotalInGame})");

        if (all100)
        {
            isAltEnding = true;
            StartCoroutine(ShowCertificateRoutine());
        }
        else
        {
            isAltEnding = false;
            Debug.Log("[EndingBranchController] Normal Ending dijalankan.");
            onNormalEnding?.Invoke();
        }
    }

    // =========================================================
    // CERTIFICATE POPUP
    // =========================================================

    private IEnumerator ShowCertificateRoutine()
    {
        if (certificatePanel == null)
        {
            // Fallback: langsung jalankan alt ending tanpa popup
            onAlternativeEndingAfterCertificate?.Invoke();
            yield break;
        }

        // Isi teks sertifikat (opsional)
        if (certificateBodyText != null)
        {
            int col   = GlobalTrashManager.Instance?.TotalCollected ?? 0;
            int total = GlobalTrashManager.Instance?.TotalInGame ?? 0;
            certificateBodyText.text =
                $"Selamat!\nAnda telah mengumpulkan <b>{col}/{total}</b> sampah\n" +
                $"dan membuktikan kepedulian terhadap lingkungan Raja Ampat!\n\n" +
                $"<size=80%>Sertifikat ECO-HERO diberikan atas dedikasi Anda.</size>";
        }

        // Freeze game saat popup
        Time.timeScale = 0f;

        certificatePanel.SetActive(true);
        yield return StartCoroutine(EaseIn());

        Debug.Log("[EndingBranchController] Sertifikat ditampilkan. Menunggu pemain menutup.");
        // Lanjut dikontrol oleh CloseCertificate()
    }

    public void CloseCertificate()
    {
        StartCoroutine(EaseOutAndContinue());
    }

    private IEnumerator EaseIn()
    {
        if (certCanvasGroup == null) yield break;
        Vector3 startScale = certRect != null ? certRect.localScale * 0.85f : Vector3.one * 0.85f;
        Vector3 endScale   = certRect != null ? certRect.localScale         : Vector3.one;

        certCanvasGroup.alpha = 0f;
        if (certRect != null) certRect.localScale = startScale;

        float elapsed = 0f;
        while (elapsed < certificatePopupDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / certificatePopupDuration);
            certCanvasGroup.alpha = t;
            if (certRect != null) certRect.localScale = Vector3.Lerp(startScale, endScale, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        certCanvasGroup.alpha = 1f;
        if (certRect != null) certRect.localScale = endScale;
    }

    private IEnumerator EaseOutAndContinue()
    {
        if (certCanvasGroup != null)
        {
            float elapsed = 0f;
            float startAlpha = certCanvasGroup.alpha;
            while (elapsed < certificatePopupDuration)
            {
                float t = elapsed / certificatePopupDuration;
                certCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            certCanvasGroup.alpha = 0f;
        }

        if (certificatePanel != null) certificatePanel.SetActive(false);
        Time.timeScale = 1f;

        onCertificateClosed?.Invoke();

        if (isAltEnding)
        {
            Debug.Log("[EndingBranchController] Alternative Ending (100%) dijalankan setelah sertifikat.");
            onAlternativeEndingAfterCertificate?.Invoke();
        }
    }
}
