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
        panelRect.sizeDelta = new Vector2(615f, 150f);

        UnityEngine.UI.CanvasScaler scaler = GetComponentInParent<UnityEngine.UI.CanvasScaler>();
        bool isLowResCanvas = scaler != null &&
                              scaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                              scaler.referenceResolution.x <= 1000f;

        if (isLowResCanvas)
        {
            panelRect.localScale = new Vector3(0.3125f, 0.3125f, 0.3125f);
            visiblePosition = new Vector2(140f, -18.75f);
            hiddenPosition = new Vector2(-220f, -18.75f);
        }
        else
        {
            panelRect.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            visiblePosition = new Vector2(336f, -45f);
            hiddenPosition = new Vector2(-500f, -45f);
        }
    }

    /// <summary>
    /// Panggil fungsi ini dari skrip Quest atau Dialog ketika quest baru diterima
    /// </summary>
    public void ShowQuestPopup()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

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