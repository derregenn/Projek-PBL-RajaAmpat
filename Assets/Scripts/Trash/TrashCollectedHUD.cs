using System.Collections;
using UnityEngine;

public static class TrashCollectedHUD
{
    private const string HUDName = "TrashCollectedUI";
    private static Coroutine activeCoroutine;
    private static MonoBehaviour coroutineHost;

    public static void Show(float displayDuration = 2.5f)
    {
        GameObject target = FindTarget();
        if (target == null)
        {
            return;
        }

        if (coroutineHost == null)
        {
            GameObject hostObject = new GameObject("TrashCollectedHUDRuntime");
            Object.DontDestroyOnLoad(hostObject);
            coroutineHost = hostObject.AddComponent<TrashCollectedHUDHost>();
        }

        if (activeCoroutine != null)
        {
            coroutineHost.StopCoroutine(activeCoroutine);
        }

        activeCoroutine = coroutineHost.StartCoroutine(Play(target, displayDuration));
    }

    private static GameObject FindTarget()
    {
        foreach (SpriteRenderer renderer in
                 Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (renderer.gameObject.name == HUDName)
                return renderer.gameObject;
        }

        foreach (RectTransform rectTransform in
                 Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (rectTransform.gameObject.name == HUDName)
                return rectTransform.gameObject;
        }

        return null;
    }

    private static IEnumerator Play(GameObject target, float displayDuration)
    {
        SpriteRenderer spriteRenderer = target.GetComponent<SpriteRenderer>();
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        RectTransform rectTransform = target.GetComponent<RectTransform>();
        Vector3 originalScale = target.transform.localScale;
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        Vector2 originalPosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;

        target.SetActive(true);

        if (spriteRenderer != null)
        {
            Color color = originalColor;
            color.a = 1f;
            spriteRenderer.color = color;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
        }

        yield return new WaitForSecondsRealtime(displayDuration);

        if (target != null)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalPosition;
            }

            target.transform.localScale = originalScale;
            target.SetActive(false);
        }

        activeCoroutine = null;
    }

    private sealed class TrashCollectedHUDHost : MonoBehaviour
    {
    }
}
