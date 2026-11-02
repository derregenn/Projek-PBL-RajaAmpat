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

    private Coroutine slideCoroutine;

    private void Start()
    {
        if (panelRect == null) panelRect = GetComponent<RectTransform>();
        if (sfxAudioSource == null) sfxAudioSource = GetComponent<AudioSource>();

        // Pastikan saat game mulai, panel berada di posisi tersembunyi
        panelRect.anchoredPosition = hiddenPosition;
    }

    /// <summary>
    /// Panggil fungsi ini dari skrip Quest atau Dialog ketika quest baru diterima
    /// </summary>
    public void ShowQuestPopup()
    {
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