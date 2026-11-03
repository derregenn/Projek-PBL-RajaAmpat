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
    public float typingSpeed = 0.03f;   // 🔴 Kecepatan ketikan per huruf

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
    private Coroutine typingCoroutine;

    public void ShowAnswerFeedback(bool isCorrect)
    {
        if (answerFeedbackText == null) return;

        answerFeedbackText.text = isCorrect ? correctAnswerFeedback : incorrectAnswerFeedback;
        answerFeedbackText.color = isCorrect ? Color.green : Color.red;
        answerFeedbackText.gameObject.SetActive(true);
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
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartPortraitCutscene(Sprite[] portraits, string[] dialogues, string sceneToLoad)
    {
        currentPortraits = portraits;
        currentDialogues = dialogues;
        nextSceneName = sceneToLoad;
        portraitIndex = 0;
        isTransitioning = false;

        SetFadeAlpha(0f);

        portraitDisplay.gameObject.SetActive(true);
        if (dialogueBox != null) dialogueBox.SetActive(true);

        cutsceneCanvas.SetActive(true);
        ShowNextPortrait();
    }

    void Update()
    {
        if (isTransitioning) return;

        // BISA KLIK KIRI / SPACE / ENTER
        if (cutsceneCanvas.activeSelf && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            // 🔴 Logika Pintar: 
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
    }

    private void ShowNextPortrait()
    {
        if (portraitIndex < currentPortraits.Length)
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
        if (portraitDisplay.sprite != null && portraitDisplay.color.a > 0)
        {
            yield return StartCoroutine(FadeAlpha(portraitDisplay, 1f, 0f, 0.3f));
        }

        // Reset teks dan pasang gambar baru
        portraitDisplay.sprite = nextSprite;
        if (dialogueText != null) dialogueText.text = "";

        // Fade in gambar baru
        yield return StartCoroutine(FadeAlpha(portraitDisplay, 0f, 1f, 0.3f));

        isTransitioning = false;

        // 🔴 Mulai Efek Ketikan setelah Gambar Selesai Fade In
        if (dialogueText != null && !string.IsNullOrEmpty(nextText))
        {
            currentFullText = nextText;
            typingCoroutine = StartCoroutine(TypewriterText(nextText));
        }
    }

    // 🔴 Coroutine untuk Mengetik Teks Karakter per Karakter
    private IEnumerator TypewriterText(string textToType)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in textToType.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
    }

    // 🔴 Fungsi Instan untuk Memunculkan Seluruh Teks saat Pemain Mengklik Cepat
    private void CompleteTyping()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (dialogueText != null) dialogueText.text = currentFullText;
        isTyping = false;
    }

    private IEnumerator EndCutsceneAndLoadScene()
    {
        isTransitioning = true;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        isTyping = false;

        // Fade In Layar Hitam
        yield return StartCoroutine(FadeAlpha(fadeBlackScreen, 0f, 1f, fadeDuration));

        portraitDisplay.gameObject.SetActive(false);
        portraitDisplay.sprite = null;
        if (dialogueBox != null) dialogueBox.SetActive(false);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Fade Out Layar Hitam
        yield return StartCoroutine(FadeAlpha(fadeBlackScreen, 1f, 0f, fadeDuration));

        portraitDisplay.gameObject.SetActive(true);
        if (dialogueBox != null) dialogueBox.SetActive(true);
        cutsceneCanvas.SetActive(false);
        isTransitioning = false;
    }

    private IEnumerator FadeAlpha(Image img, float startAlpha, float targetAlpha, float duration)
    {
        float time = 0;
        Color c = img.color;

        while (time < duration)
        {
            time += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
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