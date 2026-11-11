using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class UniversalFader : MonoBehaviour
{
    private static UniversalFader _instance;
    public static UniversalFader Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<UniversalFader>();
                if (_instance == null)
                {
                    _instance = CreateRuntimeFader();
                }
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (_instance == null)
        {
            _instance = FindFirstObjectByType<UniversalFader>();
            if (_instance == null)
            {
                _instance = CreateRuntimeFader();
            }
        }
    }

    private static UniversalFader CreateRuntimeFader()
    {
        GameObject existing = GameObject.Find("Canvas_Fader");
        if (existing != null)
        {
            var f = existing.GetComponent<UniversalFader>();
            if (f != null) return f;
        }

        GameObject canvasObj = new GameObject("Canvas_Fader");
        DontDestroyOnLoad(canvasObj);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();
        CanvasGroup cg = canvasObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;

        UniversalFader fader = canvasObj.AddComponent<UniversalFader>();
        fader.fadeCanvasGroup = cg;

        GameObject panelObj = new GameObject("FadePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        Image img = panelObj.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;

        return fader;
    }

    [Header("Referensi UI")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Pengaturan Waktu")]
    [SerializeField] private float defaultFadeDuration = 0.5f;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (fadeCanvasGroup == null)
        {
            fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Setiap kali scene baru dimuat, mulai dari layar hitam lalu fade in ke terang
        FadeInOnSceneStart();
    }

    private void Start()
    {
        // Pastikan fade in berjalan saat awal mula scene pertama dijalankan
        FadeInOnSceneStart();
    }

    private void FadeInOnSceneStart()
    {
        if (fadeCanvasGroup == null)
        {
            fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.blocksRaycasts = true;
            StopAllCoroutines();
            StartCoroutine(FadeInRoutine(defaultFadeDuration > 0 ? defaultFadeDuration : 0.8f));
        }
    }

    /// <summary>
    /// Memudarkan layar ke gelap, melakukan aksi rahasia, lalu memudarkan ke terang.
    /// </summary>
    /// <param name="midFadeAction">Fungsi yang dijalankan saat layar gelap total (misal: mematikan NPC)</param>
    public void FadeTransition(Action midFadeAction, float fadeDuration = -1f, float waitTime = 0.2f)
    {
        if (fadeDuration < 0) fadeDuration = defaultFadeDuration;
        if (fadeCanvasGroup == null)
        {
            fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();
        }
        if (fadeCanvasGroup == null)
        {
            midFadeAction?.Invoke();
            return;
        }

        StopAllCoroutines();
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
            time += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(time / duration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        // 3. JALANKAN AKSI (Matikan NPC, Teleport Player, dll)
        try
        {
            midFadeAction?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UniversalFader] Error in midFadeAction: {ex}");
        }

        // 4. Tunggu sebentar dalam keadaan gelap
        yield return new WaitForSecondsRealtime(waitTime);

        // 5. FADE IN (Gelap ke Terang)
        time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (time / duration));
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;

        // 6. Buka kembali kunci layar
        fadeCanvasGroup.blocksRaycasts = false;
    }

    public IEnumerator FadeOutRoutine(float duration = -1f)
    {
        if (duration < 0) duration = defaultFadeDuration;
        if (fadeCanvasGroup == null) fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.blocksRaycasts = true;
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(time / duration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;
    }

    public IEnumerator FadeInRoutine(float duration = -1f)
    {
        if (duration < 0) duration = defaultFadeDuration;
        if (fadeCanvasGroup == null) fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();
        if (fadeCanvasGroup == null) yield break;

        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (time / duration));
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }

    public void FadeToScene(string targetSceneName, float fadeDuration = -1f)
    {
        if (fadeDuration < 0) fadeDuration = defaultFadeDuration;
        FadeTransition(() =>
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
        }, fadeDuration, 0.1f);
    }
}
