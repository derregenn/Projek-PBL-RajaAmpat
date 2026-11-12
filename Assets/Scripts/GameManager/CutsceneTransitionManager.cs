using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class CutsceneTransitionManager : MonoBehaviour
{
    public static CutsceneTransitionManager Instance;

    [Header("UI Components")]
    public GameObject cutsceneCanvas;
    public Image portraitDisplay;       // Gambar ilustrasi pesawat/lokasi
    public GameObject dialogueBox;     // Panel/Box Dialog
    public TMP_Text dialogueText;       // Komponen Teks Narasi
    public TMP_Text answerFeedbackText; // Teks feedback jawaban benar/salah
    public string correctAnswerFeedback = "Benar!";
    public string incorrectAnswerFeedback = "Salah!";

    [Header("Typewriter Settings")]
    [Tooltip("Kecepatan munculnya huruf (semakin kecil nilainya, semakin cepat)")]
    public float typingSpeed = 0.03f;   // Kecepatan ketikan per huruf

    [Header("Fade Settings")]
    public Image fadeBlackScreen;
    public float fadeDuration = 1.0f;

    private Sprite[] currentPortraits;
    private string[] currentDialogues;
    private int portraitIndex = 0;
    private string nextSceneName;
    private bool isTransitioning = false;

    // Status Efek Ketikan
    private bool isTyping = false;
    private string currentFullText = "";
    private float nextAllowedActionTime = 0f;
    private int lastActionFrame = -1;
    private Coroutine typingCoroutine;

    public void ShowAnswerFeedback(bool isCorrect)
    {
        if (answerFeedbackText == null) return;

        answerFeedbackText.text = isCorrect ? correctAnswerFeedback : incorrectAnswerFeedback;
        answerFeedbackText.color = isCorrect ? Color.green : Color.red;
        answerFeedbackText.gameObject.SetActive(true);
    }

    void OnValidate()
    {
        EnsureDialogueBoxLayout();
    }

    void OnEnable()
    {
        EnsureDialogueBoxLayout();
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            if (cutsceneCanvas != null)
            {
                DontDestroyOnLoad(cutsceneCanvas);
            }
        }
        else if (Instance != this)
        {
            // Jika instance sudah ada dari Main Menu, sinkronkan referensi UI lokal dari scene ini
            if (cutsceneCanvas != null)
            {
                Instance.cutsceneCanvas = cutsceneCanvas;
                Instance.portraitDisplay = portraitDisplay;
                Instance.dialogueBox = dialogueBox;
                Instance.dialogueText = dialogueText;
                Instance.fadeBlackScreen = fadeBlackScreen;
                Instance.EnsureDialogueBoxLayout();
            }
            Destroy(gameObject);
            return;
        }

        EnsureDialogueBoxLayout();
    }

    public void EnsureDialogueBoxLayout()
    {
        if (dialogueBox != null)
        {
            RectTransform boxRt = dialogueBox.GetComponent<RectTransform>();
            if (boxRt != null)
            {
                // Bikin dialogbox memanjang horizontal di bagian bawah tengah layar
                boxRt.anchorMin = new Vector2(0.5f, 0f);
                boxRt.anchorMax = new Vector2(0.5f, 0f);
                boxRt.pivot = new Vector2(0.5f, 0f);
                boxRt.anchoredPosition = new Vector2(0f, 40f);
                boxRt.sizeDelta = new Vector2(1400f, 160f);
            }
        }

        if (dialogueText != null)
        {
            RectTransform textRt = dialogueText.GetComponent<RectTransform>();
            if (textRt != null)
            {
                textRt.anchorMin = new Vector2(0f, 0f);
                textRt.anchorMax = new Vector2(1f, 1f);
                textRt.pivot = new Vector2(0.5f, 0.5f);
                textRt.offsetMin = new Vector2(40f, 20f);
                textRt.offsetMax = new Vector2(-40f, -20f);
            }
            dialogueText.enableWordWrapping = true;
            dialogueText.alignment = TextAlignmentOptions.MidlineLeft;
            dialogueText.fontSize = 24f;
        }
    }

    public void StartPortraitCutscene(Sprite[] portraits, string[] dialogues, string sceneToLoad)
    {
        EnsureDialogueBoxLayout();
        currentPortraits = portraits;
        currentDialogues = dialogues;
        nextSceneName = sceneToLoad;
        portraitIndex = 0;
        isTransitioning = false;

        SetFadeAlpha(0f);

        if (portraitDisplay != null) portraitDisplay.gameObject.SetActive(true);
        if (dialogueBox != null) dialogueBox.SetActive(true);

        if (cutsceneCanvas != null) cutsceneCanvas.SetActive(true);
        ShowNextPortrait();
    }

    public void OnNextButtonClicked()
    {
        if (isTransitioning) return;
        if (Time.unscaledTime < nextAllowedActionTime) return;
        if (Time.frameCount == lastActionFrame) return; // Mencegah double-fire di frame yang sama antara Button OnClick dan Update()
        lastActionFrame = Time.frameCount;

        // Jika teks SEDANG diketik -> Selesaikan teks seketika
        // Jika teks SUDAH selesai -> Lanjut ke portrait berikutnya
        if (isTyping)
        {
            CompleteTyping();
        }
        else
        {
            ShowNextPortrait();
        }
    }

    void Update()
    {
        if (isTransitioning) return;
        if (Time.unscaledTime < nextAllowedActionTime) return;

        // BISA KLIK KIRI / SPACE / ENTER / TOMBOL E
        if (cutsceneCanvas != null && cutsceneCanvas.activeSelf)
        {
            bool pointerOverUi = UnityEngine.EventSystems.EventSystem.current != null && 
                                 UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            if (Input.GetKeyDown(KeyCode.E) || 
                Input.GetKeyDown(KeyCode.Space) || 
                Input.GetKeyDown(KeyCode.Return) || 
                (Input.GetMouseButtonDown(0) && !pointerOverUi))
            {
                OnNextButtonClicked();
            }
        }
    }

    private void ShowNextPortrait()
    {
        EnsureDialogueBoxLayout();
        nextAllowedActionTime = Time.unscaledTime + 0.35f;

        if (currentPortraits != null && portraitIndex < currentPortraits.Length)
        {
            string txt = (currentDialogues != null && portraitIndex < currentDialogues.Length) ? currentDialogues[portraitIndex] : "";
            StartCoroutine(FadeToNextPortrait(currentPortraits[portraitIndex], txt));
            portraitIndex++;
        }
        else
        {
            StartCoroutine(EndCutsceneAndLoadScene());
        }
    }

    private IEnumerator FadeToNextPortrait(Sprite nextSprite, string nextText)
    {
        isTransitioning = true;

        // Selesaikan/hentikan coroutine ketikan lama jika ada
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        isTyping = false;

        // Fade out gambar lama
        if (portraitDisplay != null && portraitDisplay.sprite != null && portraitDisplay.color.a > 0)
        {
            yield return StartCoroutine(FadeAlpha(portraitDisplay, 1f, 0f, 0.25f));
        }

        // Reset teks dan pasang gambar baru
        if (portraitDisplay != null) portraitDisplay.sprite = nextSprite;
        if (dialogueText != null) dialogueText.text = "";

        EnsureDialogueBoxLayout();

        // Fade in gambar baru
        if (portraitDisplay != null && nextSprite != null)
        {
            yield return StartCoroutine(FadeAlpha(portraitDisplay, 0f, 1f, 0.25f));
        }

        isTransitioning = false;
        nextAllowedActionTime = Time.unscaledTime + 0.2f;

        // Mulai Efek Ketikan setelah Gambar Selesai Fade In
        if (dialogueText != null && !string.IsNullOrEmpty(nextText))
        {
            currentFullText = nextText;
            typingCoroutine = StartCoroutine(TypewriterText(nextText));
        }
    }

    // Coroutine untuk Mengetik Teks Karakter per Karakter
    private IEnumerator TypewriterText(string textToType)
    {
        isTyping = true;
        if (dialogueText != null) dialogueText.text = "";

        foreach (char letter in textToType.ToCharArray())
        {
            if (dialogueText != null) dialogueText.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
        nextAllowedActionTime = Time.unscaledTime + 0.2f;
    }

    // Fungsi Instan untuk Memunculkan Seluruh Teks saat Pemain Mengklik Cepat
    private void CompleteTyping()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (dialogueText != null) dialogueText.text = currentFullText;
        isTyping = false;
        nextAllowedActionTime = Time.unscaledTime + 0.3f;
    }

    private IEnumerator EndCutsceneAndLoadScene()
    {
        isTransitioning = true;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        isTyping = false;

        // Fade In Layar Hitam
        if (fadeBlackScreen != null)
        {
            yield return StartCoroutine(FadeAlpha(fadeBlackScreen, 0f, 1f, fadeDuration));
        }

        if (portraitDisplay != null)
        {
            portraitDisplay.gameObject.SetActive(false);
            portraitDisplay.sprite = null;
        }
        if (dialogueBox != null) dialogueBox.SetActive(false);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Fade Out Layar Hitam
        if (fadeBlackScreen != null)
        {
            yield return StartCoroutine(FadeAlpha(fadeBlackScreen, 1f, 0f, fadeDuration));
        }

        if (portraitDisplay != null) portraitDisplay.gameObject.SetActive(true);
        if (dialogueBox != null) dialogueBox.SetActive(true);
        if (cutsceneCanvas != null) cutsceneCanvas.SetActive(false);
        isTransitioning = false;
    }

    private IEnumerator FadeAlpha(Image img, float startAlpha, float targetAlpha, float duration)
    {
        if (img == null) yield break;
        float time = 0;
        Color c = img.color;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(time / duration));
            img.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        img.color = c;
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeBlackScreen != null)
        {
            Color c = fadeBlackScreen.color;
            c.a = alpha;
            fadeBlackScreen.color = c;
        }
    }
}
