using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class IslandSceneTransition : MonoBehaviour
{
    [Header("Pengaturan Scene")]
    public string targetSceneName = "02_Waisai";
    public float fadeDuration = 1.0f;

    [Header("UI Fade Settings")]
    public Image fadeBlackScreen; // Tarik FadePanel dari Hierarchy ke sini

    private CanvasGroup canvasGroup;
    private bool isTransitioning = false;

    private void Start()
    {
        if (fadeBlackScreen != null)
        {
            canvasGroup = fadeBlackScreen.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = fadeBlackScreen.GetComponentInParent<CanvasGroup>();
            }

            SetFadeAlpha(0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTransitioning) return;

        bool isPlayer = collision.CompareTag("Player") || 
                        (collision.transform.root != null && collision.transform.root.CompareTag("Player")) || 
                        collision.gameObject.name.ToLower().Contains("player");

        if (isPlayer)
        {
            StartCoroutine(FadeAndLoadScene());
        }
    }

    private IEnumerator FadeAndLoadScene()
    {
        isTransitioning = true;

        if (fadeBlackScreen == null)
        {
            Debug.LogError("[Transition Error] Slot 'Fade Black Screen' belum ditarik dari Hierarchy scene saat ini!");
            SceneManager.LoadScene(targetSceneName);
            yield break;
        }

        // Pastikan GameObject FadePanel dan parent Canvas-nya aktif
        fadeBlackScreen.gameObject.SetActive(true);
        if (fadeBlackScreen.transform.parent != null)
        {
            fadeBlackScreen.transform.parent.gameObject.SetActive(true);
        }

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / fadeDuration);
            SetFadeAlpha(alpha);
            yield return null;
        }

        SetFadeAlpha(1f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGame();
        }

        SceneManager.LoadScene(targetSceneName);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }

        if (fadeBlackScreen != null)
        {
            Color c = fadeBlackScreen.color;
            c.a = alpha;
            fadeBlackScreen.color = c;
        }
    }
}