using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class IslandSceneTransition : MonoBehaviour
{
    [Header("Pengaturan Scene")]
    public string targetSceneName = "02_Waisai";
    public float fadeDuration = 1.0f;

    [Header("Ending & Credits (Khusus Pesawat Airstrip Ending)")]
    [SerializeField] private bool markGameCompletedOnTransition = false;
    [SerializeField] private bool showEndCreditsBeforeLoad = false;
    [SerializeField] private float creditsDuration = 6.0f;

    [Header("UI Fade Settings")]
    public Image fadeBlackScreen; // Tarik FadePanel dari Hierarchy ke sini

    [Header("Interaksi")]
    public bool requirePressE = true;
    public GameObject promptPanel; // Panel petunjuk untuk menekan E

    private CanvasGroup canvasGroup;
    private bool isTransitioning = false;
    private bool playerInRange = false;

    private void Start()
    {
        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }

        if (fadeBlackScreen != null)
        {
            canvasGroup = fadeBlackScreen.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = fadeBlackScreen.GetComponentInParent<CanvasGroup>();
            }

            ScreenFader fader = fadeBlackScreen.GetComponent<ScreenFader>();
            if (fader == null) fader = fadeBlackScreen.GetComponentInParent<ScreenFader>();
            if (fader != null && fader.enabled && fader.FadeInOnStart)
            {
                return;
            }

            SetFadeAlpha(0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTransitioning) return;

        if (IsPlayer(collision))
        {
            playerInRange = true;
            if (requirePressE)
            {
                if (promptPanel != null) promptPanel.SetActive(true);
            }
            else
            {
                StartCoroutine(FadeAndLoadScene());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsPlayer(collision))
        {
            playerInRange = false;
            if (promptPanel != null) promptPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (requirePressE && playerInRange && !isTransitioning && Input.GetKeyDown(KeyCode.E))
        {
            if (promptPanel != null) promptPanel.SetActive(false);
            StartCoroutine(FadeAndLoadScene());
        }
    }

    private bool IsPlayer(Collider2D collision)
    {
        return collision.CompareTag("Player") ||
               (collision.transform.root != null && collision.transform.root.CompareTag("Player")) ||
               collision.gameObject.name.ToLower().Contains("player");
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

        if (QuestController.Instance != null)
        {
            QuestController.Instance.CompleteAllActiveQuests();
        }

        if (markGameCompletedOnTransition)
        {
            PlayerPrefs.SetInt("GameCompleted", 1);
            PlayerPrefs.Save();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGame();
        }

        if (showEndCreditsBeforeLoad)
        {
            yield return StartCoroutine(ShowEndCreditsRoutine());
        }

        SceneManager.LoadScene(targetSceneName);
    }

    private IEnumerator ShowEndCreditsRoutine()
    {
        if (fadeBlackScreen == null)
        {
            yield return new WaitForSeconds(Mathf.Min(creditsDuration, 2f));
            yield break;
        }

        GameObject creditObj = new GameObject("EndCreditsOverlay", typeof(RectTransform), typeof(TextMeshProUGUI));
        creditObj.transform.SetParent(fadeBlackScreen.transform, false);

        RectTransform rt = creditObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.1f);
        rt.anchorMax = new Vector2(0.9f, 0.9f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = creditObj.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 28f;
        tmp.color = Color.white;
        tmp.text =
            "<size=42><b>RAJA AMPAT: JEJAK EKOWISATA</b></size>\n\n" +
            "Terima kasih telah menjelajahi keindahan alam dan kearifan lokal Raja Ampat.\n" +
            "Mari terus menjaga laut dan alam kita tetap lestari!\n\n" +
            "<size=22><i>Tekan [E], [Space], atau klik untuk kembali ke Main Menu</i></size>";

        float elapsed = 0f;
        float minWait = 0.5f;
        while (elapsed < creditsDuration)
        {
            elapsed += Time.deltaTime;
            if (elapsed > minWait && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
            {
                break;
            }
            yield return null;
        }

        Destroy(creditObj);
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