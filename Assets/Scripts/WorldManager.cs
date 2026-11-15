using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldMapManager : MonoBehaviour
{
    [Header("UI Reference")]
    public CanvasGroup mapCanvasGroup; // Masukkan MapMenuPanel di sini

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.M;
    public float fadeDuration = 0.25f; // Durasi transisi fade (detik)

    private bool isMapOpen = false;
    private Coroutine currentFadeRoutine;

    void Start()
    {
        // Inisialisasi awal: panel tertutup dan transparan
        if (mapCanvasGroup != null)
        {
            mapCanvasGroup.alpha = 0f;
            mapCanvasGroup.interactable = false;
            mapCanvasGroup.blocksRaycasts = false;
            mapCanvasGroup.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMap();
        }
    }

    public void ToggleMap()
    {
        isMapOpen = !isMapOpen;

        if (currentFadeRoutine != null)
        {
            StopCoroutine(currentFadeRoutine);
        }

        currentFadeRoutine = StartCoroutine(FadeRoutine(isMapOpen));
    }

    private IEnumerator FadeRoutine(bool open)
    {
        if (open)
        {
            mapCanvasGroup.gameObject.SetActive(true);
        }

        float startAlpha = mapCanvasGroup.alpha;
        float targetAlpha = open ? 1f : 0f;
        float elapsed = 0f;

        // Gunakan unscaledDeltaTime agar animasi tetap berjalan saat game pause (timeScale = 0)
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            mapCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        mapCanvasGroup.alpha = targetAlpha;

        // Kunci interaksi tombol saat panel tertutup
        mapCanvasGroup.interactable = open;
        mapCanvasGroup.blocksRaycasts = open;

        if (!open)
        {
            mapCanvasGroup.gameObject.SetActive(false);
            Time.timeScale = 1f; // Lanjutkan game setelah map benar-benar tertutup
        }
        else
        {
            Time.timeScale = 0f; // Hentikan game setelah map terbuka
        }
    }

    public void LoadIslandScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}