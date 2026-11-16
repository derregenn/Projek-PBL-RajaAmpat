using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private bool fadeInOnStart = true;

    private Coroutine currentFadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        if (fadeInOnStart && canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            FadeIn();
        }
    }

    public void FadeOut()
    {
        StartFade(1f);
    }

    public void FadeIn()
    {
        StartFade(0f);
    }

    public void FadeOutAndLoadScene(string sceneName)
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }

        currentFadeCoroutine = StartCoroutine(FadeOutAndLoadSceneRoutine(sceneName));
    }

    private IEnumerator FadeOutAndLoadSceneRoutine(string sceneName)
    {
        yield return FadeRoutine(1f);
        SceneManager.LoadScene(sceneName);
    }

    // --- TAMBAHAN METHOD UNTUK COROUTINE INTERAL/EXTERNAL ---
    public IEnumerator FadeOutRoutine()
    {
        yield return FadeRoutine(1f);
    }

    public IEnumerator FadeInRoutine()
    {
        yield return FadeRoutine(0f);
    }

    private void StartFade(float targetAlpha)
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }

        currentFadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        canvasGroup.blocksRaycasts = true;

        if (fadeDuration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            canvasGroup.blocksRaycasts = targetAlpha > 0.05f;
            yield break;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.blocksRaycasts = targetAlpha > 0.05f;
    }
}