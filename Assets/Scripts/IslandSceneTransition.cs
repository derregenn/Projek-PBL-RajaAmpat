using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class IslandSceneTransition : MonoBehaviour, IInteractable
{
    [Header("Pengaturan Scene")]
    public string targetSceneName = "02_Waisai";
    public float fadeDuration = 1.0f;

    [Header("Ending & Credits (Khusus Pesawat Airstrip Ending)")]
    [SerializeField] private bool markGameCompletedOnTransition = false;
    [SerializeField] private bool showEndCreditsBeforeLoad = false;
    [SerializeField] private float creditsDuration = 10.0f;
    [SerializeField] private Sprite epilogCutsceneSprite;
    [TextArea(2, 4)]
    [SerializeField] private string epilogCutsceneText = "";
    [SerializeField] private float epilogMinDisplayDuration = 0.5f;
    [SerializeField] private Sprite creditsPanelSprite;

    [Header("Objective Tracker & Prompt (Opsional)")]
    [SerializeField] private string objectivePromptOnEnable = "";
    [SerializeField] private string interactPromptText = "";

    [Header("UI Fade Settings")]
    public Image fadeBlackScreen; // Tarik FadePanel dari Hierarchy ke sini

    [Header("Interaksi")]
    public bool requirePressE = true;
    public GameObject promptPanel; // Panel petunjuk untuk menekan E

    private CanvasGroup canvasGroup;
    private Collider2D triggerCollider;
    private bool isTransitioning = false;
    private bool playerInRange = false;
    private bool hasPromptOverrideText = false;
    private TMP_Text promptLabel;
    private string cachedDefaultPromptLabel = "";

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        if (promptPanel != null)
        {
            promptLabel = promptPanel.GetComponentInChildren<TMP_Text>(true);
            if (promptLabel != null)
            {
                cachedDefaultPromptLabel = promptLabel.text;
            }
        }
    }

    private void OnEnable()
    {
        if (!string.IsNullOrWhiteSpace(objectivePromptOnEnable))
        {
            ShowObjectiveTracker(objectivePromptOnEnable);
            StartCoroutine(RefreshObjectiveTrackerNextFrame(objectivePromptOnEnable));
        }

        if (triggerCollider == null) triggerCollider = GetComponent<Collider2D>();
        CheckPlayerOverlapImmediate();
    }

    private IEnumerator RefreshObjectiveTrackerNextFrame(string title)
    {
        yield return null;
        if (!isTransitioning && gameObject.activeInHierarchy)
        {
            ShowObjectiveTracker(title);
        }
    }

    private static void ShowObjectiveTracker(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return;
        QuestUI qui = FindFirstObjectByType<QuestUI>(FindObjectsInactive.Include);
        if (qui != null)
        {
            qui.ShowStandalonePrompt(title);
        }
    }

    private void CheckPlayerOverlapImmediate()
    {
        if (triggerCollider == null || !triggerCollider.enabled) return;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        if (playerObj == null) return;

        Collider2D playerCol = playerObj.GetComponent<Collider2D>();
        if (playerCol != null && triggerCollider.bounds.Intersects(playerCol.bounds))
        {
            playerInRange = true;
        }
    }

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

    public string GetPromptText()
    {
        if (!isActiveAndEnabled || !requirePressE || isTransitioning) return "";
        return string.IsNullOrWhiteSpace(interactPromptText) ? "Masuk" : interactPromptText;
    }

    public void Interact()
    {
        if (isTransitioning || InteractionInputLock.IsAnyModalBlockingWorldInteract) return;
        HideLocalPrompt();
        InteractionInputLock.ConsumeInteractKeyThisFrame();
        StartCoroutine(FadeAndLoadScene());
    }

    private void ShowLocalPrompt()
    {
        if (promptPanel == null) return;
        promptPanel.SetActive(true);
    }

    private void HideLocalPrompt()
    {
        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
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
                ShowLocalPrompt();
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
            HideLocalPrompt();
        }
    }

    private void Update()
    {
        if (requirePressE && playerInRange && !isTransitioning
            && !InteractionInputLock.IsAnyModalBlockingWorldInteract
            && !InteractionInputLock.IsSignboardOpen
            && !InteractionInputLock.WasInteractKeyConsumedThisFrame
            && Input.GetKeyDown(KeyCode.E))
        {
            HideLocalPrompt();
            InteractionInputLock.ConsumeInteractKeyThisFrame();
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
        HideLocalPrompt();

        Player p = FindFirstObjectByType<Player>();
        if (p != null) p.isMovementLocked = true;

        QuestUI qui = FindFirstObjectByType<QuestUI>();
        if (qui != null && qui.panelHUD != null)
        {
            qui.panelHUD.SetActive(false);
        }

        if (epilogCutsceneSprite != null)
        {
            yield return StartCoroutine(PlayEpilogSingleFadeAndCreditsRoutine());
            yield break;
        }

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
        PersistEndingCompletion();

        if (showEndCreditsBeforeLoad)
        {
            yield return StartCoroutine(ShowEndCreditsRoutine());
        }

        SceneManager.LoadScene(targetSceneName);
    }

    private void PersistEndingCompletion()
    {
        if (QuestController.Instance != null)
        {
            QuestController.Instance.CompleteAllActiveQuests();
        }

        if (markGameCompletedOnTransition)
        {
            bool isAlt = GlobalTrashManager.Instance != null && GlobalTrashManager.Instance.IsAllTrashCollected();
            PlayerPrefs.SetInt(isAlt ? "AltEndingReached" : "NormalEndingReached", 1);
            PlayerPrefs.SetInt("GameCompleted", 1);
            PlayerPrefs.Save();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGame();
        }

        if (markGameCompletedOnTransition)
        {
            PlayerPrefs.SetString("SavedScene", "02_Waisai");
            PlayerPrefs.SetInt("ShouldRestoreSavedPosition", 0);
            PlayerPrefs.DeleteKey("PlayerX");
            PlayerPrefs.DeleteKey("PlayerY");
            PlayerPrefs.DeleteKey("PlayerZ");
            PlayerPrefs.Save();
        }
    }

    private IEnumerator PlayEpilogSingleFadeAndCreditsRoutine()
    {
        GameObject overlayRoot = new GameObject(
            "EpilogEndingOverlayCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = overlayRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = overlayRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Layer 1: Epilog_Airstrip Cutscene Image
        GameObject epilogObj = new GameObject("EpilogImage", typeof(RectTransform), typeof(Image));
        epilogObj.transform.SetParent(overlayRoot.transform, false);
        RectTransform epilogRt = epilogObj.GetComponent<RectTransform>();
        epilogRt.anchorMin = Vector2.zero;
        epilogRt.anchorMax = Vector2.one;
        epilogRt.offsetMin = Vector2.zero;
        epilogRt.offsetMax = Vector2.zero;

        Image epilogImg = epilogObj.GetComponent<Image>();
        epilogImg.sprite = epilogCutsceneSprite;
        epilogImg.preserveAspect = false;
        epilogImg.color = new Color(1f, 1f, 1f, 0f);
        epilogImg.raycastTarget = true;

        if (!string.IsNullOrWhiteSpace(epilogCutsceneText))
        {
            CreateCutsceneCaption(overlayRoot.transform, epilogCutsceneText);
        }

        // Layer 2: Single Black Fade Backdrop (di atas epilog, di bawah credits)
        GameObject blackObj = new GameObject("SingleBlackFade", typeof(RectTransform), typeof(Image));
        blackObj.transform.SetParent(overlayRoot.transform, false);
        RectTransform blackRt = blackObj.GetComponent<RectTransform>();
        blackRt.anchorMin = Vector2.zero;
        blackRt.anchorMax = Vector2.one;
        blackRt.offsetMin = Vector2.zero;
        blackRt.offsetMax = Vector2.zero;

        Image blackImg = blackObj.GetComponent<Image>();
        blackImg.color = new Color(0f, 0f, 0f, 0f);
        blackImg.raycastTarget = false;

        // Smoothly reveal Epilog_Airstrip
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            epilogImg.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t / 0.4f));
            yield return null;
        }
        epilogImg.color = Color.white;

        // Tunggu input Left Click untuk langsung melanjutkan dari cutscene Epilog_Airstrip
        int epilogStartFrame = Time.frameCount;
        while (true)
        {
            if (Time.frameCount > epilogStartFrame && Input.GetMouseButtonDown(0))
            {
                break;
            }
            yield return null;
        }

        // Fade hitam SATU KALI
        float fadeTime = Mathf.Max(0.25f, fadeDuration);
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            blackImg.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / fadeTime));
            yield return null;
        }
        blackImg.color = Color.black;
        epilogObj.SetActive(false);

        PersistEndingCompletion();

        // Tampilkan Credits di atas layar hitam sebelum langsung ke Main Menu
        if (showEndCreditsBeforeLoad)
        {
            if (creditsPanelSprite != null)
            {
                yield return StartCoroutine(ShowSpriteCreditsRoutine(overlayRoot.transform));
            }
            else
            {
                fadeBlackScreen = blackImg;
                canvasGroup = null;
                yield return StartCoroutine(ShowEndCreditsRoutine());
            }
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(targetSceneName);
    }

    private static void CreateCutsceneCaption(Transform parent, string caption)
    {
        GameObject hintGo = new GameObject("EpilogCaption", typeof(RectTransform), typeof(Image), typeof(TextMeshProUGUI));
        hintGo.transform.SetParent(parent, false);
        RectTransform rt = hintGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.15f, 0.04f);
        rt.anchorMax = new Vector2(0.85f, 0.16f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = hintGo.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);
        bg.raycastTarget = false;

        // TMP cannot share GO with Image; put text on child
        Destroy(hintGo.GetComponent<TextMeshProUGUI>());
        GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(hintGo.transform, false);
        RectTransform txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(24f, 10f);
        txtRt.offsetMax = new Vector2(-24f, -10f);

        TextMeshProUGUI tmp = txtGo.GetComponent<TextMeshProUGUI>();
        tmp.text = caption;
        tmp.fontSize = 26f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    private IEnumerator ShowSpriteCreditsRoutine(Transform overlayParent)
    {
        GameObject panelObj = new GameObject("CreditPanelOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        panelObj.transform.SetParent(overlayParent, false);

        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(1520f, 920f);
        panelRt.anchoredPosition = Vector2.zero;

        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.sprite = creditsPanelSprite;
        panelImg.preserveAspect = true;
        panelImg.raycastTarget = true;

        CanvasGroup cg = panelObj.GetComponent<CanvasGroup>();
        cg.alpha = 0f;

        bool dismissed = false;

        // Tombol X di pojok kanan atas panel credits agar konsisten dengan Main Menu
        GameObject closeObj = new GameObject("Btn_CloseCredits", typeof(RectTransform), typeof(Image), typeof(Button));
        closeObj.transform.SetParent(panelObj.transform, false);
        RectTransform closeRt = closeObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.sizeDelta = new Vector2(140f, 140f);
        closeRt.anchoredPosition = new Vector2(-18f, -18f);

        Image closeImg = closeObj.GetComponent<Image>();
        closeImg.color = new Color(1f, 1f, 1f, 0f);
        Button closeBtn = closeObj.GetComponent<Button>();
        closeBtn.onClick.AddListener(() => dismissed = true);

        float t = 0f;
        while (t < 0.45f)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(t / 0.45f);
            yield return null;
        }
        cg.alpha = 1f;

        float elapsed = 0f;
        float waitDuration = creditsDuration > 0f ? creditsDuration : 8f;
        int creditsStartFrame = Time.frameCount;
        while (elapsed < waitDuration && !dismissed)
        {
            elapsed += Time.unscaledDeltaTime;
            if (Time.frameCount > creditsStartFrame && Input.GetMouseButtonDown(0))
            {
                dismissed = true;
            }
            yield return null;
        }

        // Pudarkan Credit Panel di atas layar hitam sebelum langsung ke Main Menu
        t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = 1f - Mathf.Clamp01(t / 0.35f);
            yield return null;
        }
        cg.alpha = 0f;
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
            "<size=22><i>Klik kiri untuk kembali ke Main Menu</i></size>";

        float elapsed = 0f;
        int creditsStartFrame = Time.frameCount;
        while (elapsed < creditsDuration)
        {
            elapsed += Time.deltaTime;
            if (Time.frameCount > creditsStartFrame && Input.GetMouseButtonDown(0))
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