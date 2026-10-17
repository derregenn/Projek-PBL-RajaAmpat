using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System; // Dibutuhkan untuk Action

public class UniversalFader : MonoBehaviour
{
    public static UniversalFader Instance { get; private set; }

    [Header("Referensi UI")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Pengaturan Waktu")]
    [SerializeField] private float defaultFadeDuration = 0.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Agar fader ini terbawa ke semua scene
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Pastikan layar terang saat game baru mulai
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// Memudarkan layar ke gelap, melakukan aksi rahasia, lalu memudarkan ke terang.
    /// </summary>
    /// <param name="midFadeAction">Fungsi yang dijalankan saat layar gelap total (misal: mematikan NPC)</param>
    public void FadeTransition(Action midFadeAction, float fadeDuration = -1f, float waitTime = 0.2f)
    {
        if (fadeDuration < 0) fadeDuration = defaultFadeDuration;
        StartCoroutine(TransitionRoutine(fadeDuration, waitTime, midFadeAction));
    }

    private IEnumerator TransitionRoutine(float duration, float waitTime, Action midFadeAction)
    {
        // 1. Kunci layar agar player tidak bisa klik apapun saat transisi
        fadeCanvasGroup.blocksRaycasts = true;

        // 2. FADE OUT (Terang ke Gelap)
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, time / duration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        // 3. JALANKAN AKSI (Matikan NPC, Teleport Player, dll)
        midFadeAction?.Invoke();

        // 4. Tunggu sebentar dalam keadaan gelap
        yield return new WaitForSeconds(waitTime);

        // 5. FADE IN (Gelap ke Terang)
        time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, time / duration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;

        // 6. Buka kembali kunci layar
        fadeCanvasGroup.blocksRaycasts = false;
    }
}