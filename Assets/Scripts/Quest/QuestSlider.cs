using System.Collections;
using UnityEngine;

public class QuestSlider : MonoBehaviour
{
    [Header("Referensi UI")]
    [SerializeField] private RectTransform panelRect;

    [Header("Pengaturan Posisi (Lihat Pos X & Pos Y di Inspector)")]
    [Tooltip("Posisi saat UI tersembunyi di luar layar (Misal X: 500, Y: -40)")]
    [SerializeField] private Vector2 hiddenPosition;

    [Tooltip("Posisi saat UI tampil di layar (Sesuai gambar kamu, misal X: -292.5, Y: -40)")]
    [SerializeField] private Vector2 visiblePosition;

    [Header("Pengaturan Waktu")]
    [Tooltip("Lama waktu animasi slide (detik)")]
    [SerializeField] private float slideDuration = 0.5f;
    [Tooltip("Lama UI diam di layar sebelum slide out (detik)")]
    [SerializeField] private float displayDuration = 3f;

    [Header("SFX Slide")]
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip slideInSfx;
    [SerializeField] private AudioClip slideOutSfx;

    [Header("Normalisasi Skala & Posisi")]
    [SerializeField] private bool normalizeTrackerLayout = true;

    private Coroutine slideCoroutine;

    private void Awake()
    {
        if (panelRect == null) panelRect = GetComponent<RectTransform>();
        if (sfxAudioSource == null) sfxAudioSource = GetComponent<AudioSource>();

        EnsureStandardTrackerLayout();

        if (panelRect != null)
        {
            // Pastikan saat game mulai, panel berada di posisi tersembunyi
            panelRect.anchoredPosition = hiddenPosition;
        }
    }

    public void EnsureStandardTrackerLayout()
    {
        if (!normalizeTrackerLayout || panelRect == null) return;

        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);

        // Ukuran standar dasar yang pas: proporsional, rapi, dan mudah dibaca
        panelRect.sizeDelta = new Vector2(600f, 130f);

        // Cari resolusi acuan dari CanvasScaler atau RectTransform Canvas
        UnityEngine.UI.CanvasScaler scaler = GetComponentInParent<UnityEngine.UI.CanvasScaler>();
        float refWidth = 1920f;
        if (scaler != null && scaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            refWidth = scaler.referenceResolution.x;
        }
        else
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                RectTransform canvasRt = parentCanvas.GetComponent<RectTransform>();
                if (canvasRt != null && canvasRt.rect.width > 0)
                {
                    refWidth = canvasRt.rect.width;
                }
            }
        }

        // Tentukan scale & posisi berdasarkan resolusi canvas
        if (refWidth <= 1000f)
        {
            // Canvas Resolusi Rendah (misal 800x600):
            // Scale 0.58 membuat panel berukuran efektif ~348x75 (~43% lebar 800px),
            // proporsional, tidak terlalu mendominasi layar, dan teks tetap jelas terbaca.
            float scaleFactor = 0.58f;
            panelRect.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);

            float effectiveHalfW = (600f * scaleFactor) * 0.5f; // ~174f
            visiblePosition = new Vector2(effectiveHalfW + 16f, -16f);
            hiddenPosition = new Vector2(-(effectiveHalfW + 45f), -16f);
        }
        else
        {
            // Canvas Resolusi Tinggi (1920x1080 Full HD, dll):
            // Scale 1.0 penuh berukuran 600x130, pas di pojok kiri atas dan teks tajam.
            panelRect.localScale = Vector3.one;

            float effectiveHalfW = 600f * 0.5f; // 300f
            visiblePosition = new Vector2(effectiveHalfW + 25f, -25f);
            hiddenPosition = new Vector2(-(effectiveHalfW + 60f), -25f);
        }

        // Pastikan durasi tampil cukup lama agar pemain dapat membaca dengan tenang
        if (displayDuration < 5f)
        {
            displayDuration = 5.5f;
        }

        // Pastikan rounded rect background memiliki corner radius yang pas dan kontras tinggi
        ProceduralRoundedRect roundedRect = GetComponent<ProceduralRoundedRect>();
        if (roundedRect != null)
        {
            roundedRect.cornerRadius = 45f;
            roundedRect.cornerSegments = 20;
            // Background gelap pekat untuk keterbacaan tinggi di berbagai latar belakang (air, pasir, rumput)
            if (roundedRect.color.a < 0.85f)
            {
                roundedRect.color = new Color(0.06f, 0.07f, 0.10f, 0.94f);
            }
        }
    }

    /// <summary>
    /// Menampilkan HUD Quest secara instan tanpa animasi slide (misal untuk testing/debugging)
    /// </summary>
    public void SnapToVisible()
    {
        if (panelRect == null) panelRect = GetComponent<RectTransform>();
        EnsureStandardTrackerLayout();
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        panelRect.anchoredPosition = visiblePosition;
    }

    /// <summary>
    /// Menyembunyikan HUD Quest secara instan tanpa animasi slide
    /// </summary>
    public void SnapToHidden()
    {
        if (panelRect == null) panelRect = GetComponent<RectTransform>();
        EnsureStandardTrackerLayout();
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        panelRect.anchoredPosition = hiddenPosition;
    }

    /// <summary>
    /// Panggil fungsi ini dari skrip Quest atau Dialog ketika quest baru diterima atau ada progres
    /// </summary>
    public void ShowQuestPopup()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (panelRect == null) panelRect = GetComponent<RectTransform>();
        EnsureStandardTrackerLayout();

        if (!isActiveAndEnabled)
        {
            Debug.LogError("[QuestSlider] Panel harus aktif di hierarchy agar popup dapat dijalankan.", this);
            return;
        }

        if (panelRect == null)
        {
            Debug.LogError("[QuestSlider] Panel RectTransform tidak ditemukan.", this);
            return;
        }

        // Hentikan animasi yang sedang berjalan (jika ada) agar tidak bentrok
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);

        // Mulai animasi masuk -> diam -> keluar
        slideCoroutine = StartCoroutine(SlideSequence());
    }

    private IEnumerator SlideSequence()
    {
        EnsureStandardTrackerLayout();

        // 1. SLIDE IN (Masuk ke layar)
        PlaySfx(slideInSfx);
        float time = 0;
        Vector2 startPos = panelRect.anchoredPosition;

        while (time < slideDuration)
        {
            time += Time.deltaTime;
            float t = time / slideDuration;

            // Rumus SmoothStep agar pergerakan melambat di ujung (tidak kaku)
            t = t * t * (3f - 2f * t);

            panelRect.anchoredPosition = Vector2.Lerp(startPos, visiblePosition, t);
            yield return null;
        }
        panelRect.anchoredPosition = visiblePosition;

        // 2. DIAM DI LAYAR
        yield return new WaitForSeconds(displayDuration);

        // 3. SLIDE OUT (Keluar layar)
        PlaySfx(slideOutSfx);
        time = 0;
        startPos = panelRect.anchoredPosition;

        while (time < slideDuration)
        {
            time += Time.deltaTime;
            float t = time / slideDuration;
            t = t * t * (3f - 2f * t);

            panelRect.anchoredPosition = Vector2.Lerp(startPos, hiddenPosition, t);
            yield return null;
        }
        panelRect.anchoredPosition = hiddenPosition;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (sfxAudioSource != null && clip != null)
            sfxAudioSource.PlayOneShot(clip);
    }
}