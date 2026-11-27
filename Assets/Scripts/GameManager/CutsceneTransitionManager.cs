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

    [Header("Dialogue Box Pop-Out Settings")]
    [Tooltip("Sembunyikan dialogueBox otomatis jika teks pada portrait kosong.")]
    public bool hideDialogueBoxWhenEmpty = true;
    [Tooltip("Opsional: Daftar index portrait (berbasis 0, mis. 2 dan 3 untuk portrait 3 & 4) yang menampilkan dialogueBox.")]
    public int[] visibleDialogueBoxPortraitIndices;
    [Tooltip("Aktifkan animasi pop-out scale dialogueBox dari 0 ke scale awal di scene.")]
    public bool enableDialogueBoxPopOut = true;
    [Tooltip("Durasi animasi pop-out scale dialogueBox dalam detik.")]
    public float dialogueBoxPopDuration = 0.25f;
    [Tooltip("Jika aktif, animasi pop-out dimainkan setiap berpindah portrait yang memiliki dialogueBox.")]
    public bool popOutOnEveryDialoguePortrait = true;

    private Sprite[] currentPortraits;
    private string[] currentDialogues;
    private int portraitIndex = 0;
    private string nextSceneName;
    private bool isTransitioning = false;
    private Vector3 dialogueBoxInitialScale = Vector3.one;
    private bool hasCachedInitialScale = false;

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

            CacheDialogueBoxInitialScale();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CacheDialogueBoxInitialScale()
    {
        if (hasCachedInitialScale || dialogueBox == null) return;
        Vector3 s = dialogueBox.transform.localScale;
        dialogueBoxInitialScale = (s.sqrMagnitude > 0.000001f) ? s : Vector3.one;
        hasCachedInitialScale = true;
    }

    public void StartPortraitCutscene(Sprite[] portraits, string[] dialogues, string sceneToLoad)
    {
        CacheDialogueBoxInitialScale();

        currentPortraits = portraits;
        currentDialogues = dialogues;
        nextSceneName = sceneToLoad;
        portraitIndex = 0;
        isTransitioning = false;

        SetFadeAlpha(0f);

        portraitDisplay.gameObject.SetActive(true);
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
            if (enableDialogueBoxPopOut)
            {
                dialogueBox.transform.localScale = Vector3.zero;
            }
        }
        if (dialogueText != null) dialogueText.text = "";

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
            int currentIndex = portraitIndex;
            string txt = (currentDialogues != null && portraitIndex < currentDialogues.Length) ? currentDialogues[portraitIndex] : "";
            StartCoroutine(FadeToNextPortrait(currentPortraits[portraitIndex], txt, currentIndex));
            portraitIndex++;
        }
        else
        {
            StartCoroutine(EndCutsceneAndLoadScene());
        }
    }

    private bool ShouldShowDialogueBoxForPortrait(int currentIndex, string nextText)
    {
        if (visibleDialogueBoxPortraitIndices != null && visibleDialogueBoxPortraitIndices.Length > 0 && currentPortraits != null)
        {
            bool hasApplicableIndex = false;
            for (int i = 0; i < visibleDialogueBoxPortraitIndices.Length; i++)
            {
                if (visibleDialogueBoxPortraitIndices[i] >= 0 && visibleDialogueBoxPortraitIndices[i] < currentPortraits.Length)
                {
                    hasApplicableIndex = true;
                    break;
                }
            }

            if (hasApplicableIndex)
            {
                for (int i = 0; i < visibleDialogueBoxPortraitIndices.Length; i++)
                {
                    if (visibleDialogueBoxPortraitIndices[i] == currentIndex)
                    {
                        return !hideDialogueBoxWhenEmpty || !string.IsNullOrWhiteSpace(nextText);
                    }
                }
                return false;
            }
        }

        return !hideDialogueBoxWhenEmpty || !string.IsNullOrWhiteSpace(nextText);
    }

    private IEnumerator FadeToNextPortrait(Sprite nextSprite, string nextText, int currentIndex)
    {
        isTransitioning = true;

        // Selesaikan/hentikan coroutine ketikan lama jika ada
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        isTyping = false;

        bool shouldShowBox = ShouldShowDialogueBoxForPortrait(currentIndex, nextText);
        if (dialogueBox != null && (!shouldShowBox || popOutOnEveryDialoguePortrait))
        {
            dialogueBox.SetActive(false);
        }

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

        if (dialogueBox != null)
        {
            if (shouldShowBox)
            {
                bool wasHidden = !dialogueBox.activeSelf;
                dialogueBox.SetActive(true);
                if (enableDialogueBoxPopOut && (wasHidden || popOutOnEveryDialoguePortrait))
                {
                    yield return StartCoroutine(PopOutDialogueBoxRoutine());
                }
                else
                {
                    dialogueBox.transform.localScale = dialogueBoxInitialScale;
                }
            }
            else
            {
                dialogueBox.SetActive(false);
            }
        }

        isTransitioning = false;

        // 🔴 Mulai Efek Ketikan setelah Gambar & Pop-Out DialogueBox Selesai
        if (shouldShowBox && dialogueText != null && !string.IsNullOrEmpty(nextText))
        {
            currentFullText = nextText;
            typingCoroutine = StartCoroutine(TypewriterText(nextText));
        }
    }

    private IEnumerator PopOutDialogueBoxRoutine()
    {
        if (dialogueBox == null) yield break;

        if (dialogueBoxPopDuration <= 0f)
        {
            dialogueBox.transform.localScale = dialogueBoxInitialScale;
            yield break;
        }

        dialogueBox.transform.localScale = Vector3.zero;
        float elapsed = 0f;
        while (elapsed < dialogueBoxPopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dialogueBoxPopDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            dialogueBox.transform.localScale = Vector3.LerpUnclamped(Vector3.zero, dialogueBoxInitialScale, eased);
            yield return null;
        }

        dialogueBox.transform.localScale = dialogueBoxInitialScale;
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

        // Sembunyikan panel sibling di bawah cutsceneCanvas (mis. background Panel Main Menu)
        // agar saat fade 1->0 setelah scene dimuat, scene baru langsung terlihat memudar masuk.
        var hiddenCutsceneChildren = new System.Collections.Generic.List<GameObject>();
        if (cutsceneCanvas != null && fadeBlackScreen != null)
        {
            Transform canvasTr = cutsceneCanvas.transform;
            Transform fadeTr = fadeBlackScreen.transform;
            for (int i = 0; i < canvasTr.childCount; i++)
            {
                Transform child = canvasTr.GetChild(i);
                if (child == fadeTr || fadeTr.IsChildOf(child)) continue;
                if (child.gameObject.activeSelf)
                {
                    hiddenCutsceneChildren.Add(child.gameObject);
                    child.gameObject.SetActive(false);
                }
            }
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Fade Out Layar Hitam
        yield return StartCoroutine(FadeAlpha(fadeBlackScreen, 1f, 0f, fadeDuration));

        for (int i = 0; i < hiddenCutsceneChildren.Count; i++)
        {
            if (hiddenCutsceneChildren[i] != null) hiddenCutsceneChildren[i].SetActive(true);
        }

        portraitDisplay.gameObject.SetActive(true);
        if (dialogueBox != null)
        {
            dialogueBox.transform.localScale = dialogueBoxInitialScale;
            dialogueBox.SetActive(false);
        }
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