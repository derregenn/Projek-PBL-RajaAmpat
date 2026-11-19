using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TriviaQuizUI : MonoBehaviour
{
    public static TriviaQuizUI Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Instance = null;
    }

    public static TriviaQuizUI FindOrActivateInstance()
    {
        if (Instance != null)
        {
            if (!Instance.gameObject.activeSelf)
            {
                Instance.gameObject.SetActive(true);
            }
            Instance.EnsureUIReferencesValid();
            return Instance;
        }

        TriviaQuizUI[] candidates = FindObjectsByType<TriviaQuizUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        TriviaQuizUI best = null;
        for (int i = 0; i < candidates.Length; i++)
        {
            TriviaQuizUI candidate = candidates[i];
            if (candidate == null) continue;
            if (best == null || (candidate.choicesPanel != null && best.choicesPanel == null))
            {
                best = candidate;
            }
        }

        if (best != null)
        {
            if (!best.gameObject.activeSelf)
            {
                best.gameObject.SetActive(true);
            }
            if (Instance == null)
            {
                Instance = best;
            }
            Instance.EnsureUIReferencesValid();
        }
        return Instance;
    }

    public static void AdoptOrAttachTo(GameObject persistentHost, TriviaQuizUI sceneSource)
    {
        if (sceneSource == null) return;

        if (Instance != null && Instance != sceneSource)
        {
            Instance.AdoptSceneReferences(sceneSource);
            return;
        }

        if (persistentHost != null && persistentHost != sceneSource.gameObject)
        {
            TriviaQuizUI hostTrivia = persistentHost.GetComponent<TriviaQuizUI>();
            if (hostTrivia == null)
            {
                hostTrivia = persistentHost.AddComponent<TriviaQuizUI>();
            }
            Instance = hostTrivia;
            hostTrivia.AdoptSceneReferences(sceneSource);
        }
    }

    public void AdoptSceneReferences(TriviaQuizUI source)
    {
        if (source == null || source == this) return;

        StopAllCoroutines();
        isQuizActive = false;
        isShowingFeedback = false;
        canPressEToClose = false;
        onQuizCompleted = null;
        feedbackOutline = null;

        if (source.choicesPanel != null) choicesPanel = source.choicesPanel;
        if (source.questionText != null) questionText = source.questionText;
        if (source.optionButtons != null && source.optionButtons.Length > 0) optionButtons = source.optionButtons;
        if (source.optionTexts != null && source.optionTexts.Length > 0) optionTexts = source.optionTexts;

        correctBorderColor = source.correctBorderColor;
        wrongBorderColor = source.wrongBorderColor;
        sfxVolume = source.sfxVolume;
        enableButtonHover = source.enableButtonHover;
        hoverScale = source.hoverScale;
        hoverTransitionSpeed = source.hoverTransitionSpeed;
        closeKey = source.closeKey;

        if (source.startQuizSFX != null) { startQuizSFX = source.startQuizSFX; PreloadAndTrimClip(ref startQuizSFX); }
        if (source.buttonHoverSFX != null) { buttonHoverSFX = source.buttonHoverSFX; PreloadAndTrimClip(ref buttonHoverSFX); }
        if (source.correctAnswerSFX != null) { correctAnswerSFX = source.correctAnswerSFX; PreloadAndTrimClip(ref correctAnswerSFX); }
        if (source.wrongAnswerSFX != null) { wrongAnswerSFX = source.wrongAnswerSFX; PreloadAndTrimClip(ref wrongAnswerSFX); }
        if (source.correctFeedbackMessages != null && source.correctFeedbackMessages.Length > 0)
        {
            correctFeedbackMessages = source.correctFeedbackMessages;
        }
        if (source.wrongFeedbackMessages != null && source.wrongFeedbackMessages.Length > 0)
        {
            wrongFeedbackMessages = source.wrongFeedbackMessages;
        }

        EnsureAudioSourceValid();

        if (choicesPanel != null) choicesPanel.SetActive(false);
        SetupFeedbackOutline();
        SetupOptionButtonHovers();
        EnsureUIReferencesValid();
    }

    [Header("References")]
    [SerializeField] private GameObject choicesPanel;      // Panel penampung 3 tombol di bawah
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private Button[] optionButtons;       // 3 Tombol Jawaban
    [SerializeField] private TextMeshProUGUI[] optionTexts;// Teks pada 3 tombol
    [SerializeField] private Color correctBorderColor = new Color(0.2f, 0.82f, 0.48f);
    [SerializeField] private Color wrongBorderColor = new Color(0.94f, 0.22f, 0.25f);

    [Header("Audio Settings (Pattern Recognition & Feedback)")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("SFX/Jingle yang diputar saat kuis dimulai agar player mengenali pattern masuknya state trivia")]
    [SerializeField] private AudioClip startQuizSFX;
    [Tooltip("SFX saat tombol pilihan jawaban di-hover oleh kursor/navigasi")]
    [SerializeField] private AudioClip buttonHoverSFX;
    [Tooltip("SFX saat jawaban benar")]
    [SerializeField] private AudioClip correctAnswerSFX;
    [Tooltip("SFX saat jawaban salah")]
    [SerializeField] private AudioClip wrongAnswerSFX;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;

    [Header("Button Hover Settings")]
    [SerializeField] private bool enableButtonHover = true;
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float hoverTransitionSpeed = 14f;

    [Header("Feedback per Question")]
    [SerializeField] private string[] correctFeedbackMessages;
    [SerializeField] private string[] wrongFeedbackMessages;

    [Header("Close Settings")]
    [SerializeField] private KeyCode closeKey = KeyCode.E;

    private TriviaQuestion[] currentQuestions;
    private int currentQuestionIndex = 0;
    private System.Action onQuizCompleted;
    private bool isQuizActive = false;
    private bool canPressEToClose = false;
    private bool isShowingFeedback = false; // Flag agar tombol tidak bisa ditekan saat feedback muncul
    private Outline feedbackOutline;

    public static bool IsAnyQuizActive =>
        Instance != null
        && Instance.isQuizActive
        && Instance.gameObject.activeInHierarchy
        && (Instance.choicesPanel == null || Instance.choicesPanel.activeInHierarchy);
    public bool IsQuizActive => isQuizActive;

    private bool IsDuplicatePersistentSingleton(out GameObject survivingHost)
    {
        QuestController qc = GetComponent<QuestController>();
        if (qc != null && QuestController.Instance != null && QuestController.Instance != qc)
        {
            survivingHost = QuestController.Instance.gameObject;
            return true;
        }

        GameManager gm = GetComponent<GameManager>();
        if (gm != null && GameManager.Instance != null && GameManager.Instance != gm)
        {
            survivingHost = GameManager.Instance.gameObject;
            return true;
        }

        survivingHost = null;
        return false;
    }

    private void Awake()
    {
        if (IsDuplicatePersistentSingleton(out GameObject survivingHost))
        {
            AdoptOrAttachTo(survivingHost, this);
            return;
        }

        // Selesaikan quiz pada instance lama sebelum mengganti Instance agar callback-nya tidak hilang.
        if (Instance != null && Instance != this)
        {
            Instance.EndQuiz();
        }

        Instance = this;

        if (choicesPanel != null) choicesPanel.SetActive(false);
        if (questionText == null && choicesPanel != null)
        {
            Transform questionTransform = choicesPanel.transform.Find("QuestionText");
            if (questionTransform != null)
            {
                questionText = questionTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        SetupFeedbackOutline();
        EnsureAudioSourceValid();

        PreloadAndTrimClip(ref startQuizSFX);
        PreloadAndTrimClip(ref buttonHoverSFX);
        PreloadAndTrimClip(ref correctAnswerSFX);
        PreloadAndTrimClip(ref wrongAnswerSFX);

        SetupOptionButtonHovers();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        isQuizActive = false;
        isShowingFeedback = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isQuizActive = false;
        isShowingFeedback = false;
        canPressEToClose = false;
        feedbackOutline = null;

        if (choicesPanel == null || HasInvalidButtonReferences() || HasInvalidTextReferences())
        {
            EnsureUIReferencesValid();
        }
    }

    private void EnsureAudioSourceValid()
    {
        if (audioSource == null || audioSource.gameObject != gameObject)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
    }

    private bool HasInvalidButtonReferences()
    {
        if (optionButtons == null || optionButtons.Length == 0) return true;
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] == null) return true;
        }
        return false;
    }

    private bool HasInvalidTextReferences()
    {
        if (optionTexts == null || optionTexts.Length == 0) return true;
        for (int i = 0; i < optionTexts.Length; i++)
        {
            if (optionTexts[i] == null) return true;
        }
        return false;
    }

    public void EnsureUIReferencesValid()
    {
        EnsureAudioSourceValid();

        if (choicesPanel != null
            && questionText != null
            && !HasInvalidButtonReferences()
            && !HasInvalidTextReferences())
        {
            return;
        }

        if (choicesPanel == null)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isLoaded)
            {
                GameObject[] roots = activeScene.GetRootGameObjects();
                for (int r = 0; r < roots.Length && choicesPanel == null; r++)
                {
                    if (roots[r] == null) continue;
                    Transform[] all = roots[r].GetComponentsInChildren<Transform>(true);
                    for (int i = 0; i < all.Length; i++)
                    {
                        if (all[i] != null && string.Equals(all[i].name, "QuizPanel", System.StringComparison.OrdinalIgnoreCase))
                        {
                            choicesPanel = all[i].gameObject;
                            break;
                        }
                    }
                }
            }
        }

        if (choicesPanel == null) return;

        Transform panelTr = choicesPanel.transform;

        if (questionText == null)
        {
            Transform qTr = panelTr.Find("QuestionBox/QuestionText") ?? panelTr.Find("QuestionText");
            if (qTr != null) questionText = qTr.GetComponent<TextMeshProUGUI>();

            if (questionText == null)
            {
                TextMeshProUGUI[] tmps = choicesPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < tmps.Length; i++)
                {
                    if (tmps[i] == null) continue;
                    bool isQuestionName = tmps[i].name.IndexOf("Question", System.StringComparison.OrdinalIgnoreCase) >= 0;
                    bool isQuestionParent = tmps[i].transform.parent != null
                        && tmps[i].transform.parent.name.IndexOf("Question", System.StringComparison.OrdinalIgnoreCase) >= 0;
                    if (isQuestionName || isQuestionParent)
                    {
                        questionText = tmps[i];
                        break;
                    }
                }
            }
        }

        if (HasInvalidButtonReferences())
        {
            Transform[] allTr = choicesPanel.GetComponentsInChildren<Transform>(true);
            Button btn1 = null, btn2 = null, btn3 = null;
            for (int i = 0; i < allTr.Length; i++)
            {
                Transform t = allTr[i];
                if (t == null) continue;
                if (t.name == "Button1") btn1 = t.GetComponent<Button>();
                else if (t.name == "Button2") btn2 = t.GetComponent<Button>();
                else if (t.name == "Button3") btn3 = t.GetComponent<Button>();
            }

            if (btn1 != null || btn2 != null || btn3 != null)
            {
                var list = new System.Collections.Generic.List<Button>(3);
                if (btn1 != null) list.Add(btn1);
                if (btn2 != null) list.Add(btn2);
                if (btn3 != null) list.Add(btn3);
                optionButtons = list.ToArray();
            }
            else
            {
                optionButtons = choicesPanel.GetComponentsInChildren<Button>(true);
            }
        }

        if (optionButtons != null && HasInvalidTextReferences())
        {
            optionTexts = new TextMeshProUGUI[optionButtons.Length];
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null)
                {
                    optionTexts[i] = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }
        }

        feedbackOutline = null;
        SetupFeedbackOutline();
        SetupOptionButtonHovers();
    }

    private static void PreloadAndTrimClip(ref AudioClip clip)
    {
        if (clip == null) return;
        if (!string.IsNullOrEmpty(clip.name) && clip.name.EndsWith("_Instant", System.StringComparison.Ordinal)) return;
        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            clip.LoadAudioData();
        }
        try
        {
            if (clip.loadState != AudioDataLoadState.Loaded || clip.samples <= 1024 || clip.channels <= 0) return;
            int totalSamples = clip.samples * clip.channels;
            float[] samples = new float[totalSamples];
            if (!clip.GetData(samples, 0)) return;

            int firstLoudSample = -1;
            for (int i = 0; i < totalSamples; i++)
            {
                if (Mathf.Abs(samples[i]) > 0.01f)
                {
                    firstLoudSample = (i / clip.channels) * clip.channels;
                    break;
                }
            }

            int minTrimFrames = 80;
            if (firstLoudSample > minTrimFrames * clip.channels && totalSamples - firstLoudSample > 512 * clip.channels)
            {
                int remainingSamples = totalSamples - firstLoudSample;
                int remainingFrames = remainingSamples / clip.channels;
                AudioClip trimmed = AudioClip.Create(clip.name + "_Instant", remainingFrames, clip.channels, clip.frequency, false);
                float[] trimmedData = new float[remainingSamples];
                System.Array.Copy(samples, firstLoudSample, trimmedData, 0, remainingSamples);
                trimmed.SetData(trimmedData, 0);
                clip = trimmed;
            }
        }
        catch (System.Exception)
        {
            // Streaming/compressed clips yang tidak mendukung GetData tetap menggunakan preload LoadAudioData
        }
    }

    private void Update()
    {
        if (isQuizActive && canPressEToClose && Input.GetKeyDown(closeKey) && !isShowingFeedback)
        {
            InteractionInputLock.ConsumeInteractKeyThisFrame();
            CloseQuiz();
        }
    }

    public void StartQuiz(TriviaQuestion[] questions, System.Action onComplete)
    {
        EnsureUIReferencesValid();
        currentQuestions = questions;
        currentQuestionIndex = 0;
        onQuizCompleted = onComplete;
        isQuizActive = true;
        isShowingFeedback = false;

        SetupOptionButtonHovers();
        StartCoroutine(EnableCloseDelay());
        ShowQuestion();
    }

    private IEnumerator EnableCloseDelay()
    {
        canPressEToClose = false;
        yield return new WaitForSeconds(0.2f);
        canPressEToClose = true;
    }

    private void ShowQuestion()
    {
        EnsureUIReferencesValid();
        SetFeedbackOutline(false, false);

        if (currentQuestions == null || currentQuestionIndex >= currentQuestions.Length)
        {
            EndQuiz();
            return;
        }

        TriviaQuestion q = currentQuestions[currentQuestionIndex];

        // Mainkan SFX setiap kali pertanyaan ditampilkan.
        PlaySFX(startQuizSFX);

        if (questionText != null) questionText.text = q.questionText;

        // Tampilkan panel soal dan opsi jawaban.
        if (choicesPanel != null) choicesPanel.SetActive(true);

        if (optionButtons == null) return;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] == null) continue;

            if (q.options != null && i < q.options.Length)
            {
                optionButtons[i].gameObject.SetActive(true);
                if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null)
                {
                    optionTexts[i].text = q.options[i];
                }

                int buttonIndex = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(buttonIndex));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnOptionSelected(int selectedIndex)
    {
        if (isShowingFeedback) return;

        ResetButtonScales();

        TriviaQuestion q = currentQuestions[currentQuestionIndex];

        if (selectedIndex == q.correctAnswerIndex)
        {
            // SFX Jawaban Benar
            PlaySFX(correctAnswerSFX);

            // Prioritaskan feedback spesifik per pertanyaan dari Inspector, baru fallback ke array/default
            string successMsg = !string.IsNullOrWhiteSpace(q.correctFeedback)
                ? q.correctFeedback
                : GetFeedbackMessage(correctFeedbackMessages, currentQuestionIndex, "Naise, jawaban kamu benar!");
            StartCoroutine(ShowFeedbackRoutine(successMsg, true));
        }
        else
        {
            // SFX Jawaban Salah
            PlaySFX(wrongAnswerSFX);

            // Prioritaskan feedback spesifik per pertanyaan dari Inspector, baru fallback ke array/default
            string wrongMsg = !string.IsNullOrWhiteSpace(q.wrongFeedback)
                ? q.wrongFeedback
                : GetFeedbackMessage(wrongFeedbackMessages, currentQuestionIndex, "Salah, coba lagi ya!");
            StartCoroutine(ShowFeedbackRoutine(wrongMsg, false));
        }
    }

    private string GetFeedbackMessage(string[] messages, int questionIndex, string defaultMessage)
    {
        if (messages != null && questionIndex < messages.Length && !string.IsNullOrWhiteSpace(messages[questionIndex]))
        {
            return messages[questionIndex];
        }

        return defaultMessage;
    }

    private IEnumerator ShowFeedbackRoutine(string feedbackMessage, bool isCorrect)
    {
        isShowingFeedback = true;
        SetFeedbackOutline(true, isCorrect);

        if (questionText != null) questionText.text = feedbackMessage;
        if (optionButtons != null)
        {
            foreach (Button button in optionButtons)
            {
                if (button != null) button.gameObject.SetActive(false);
            }
        }

        // Tunggu sebentar agar player sempat membaca feedback
        yield return new WaitForSeconds(1.5f);

        isShowingFeedback = false;

        if (isCorrect)
        {
            currentQuestionIndex++;
            ShowQuestion();
        }
        else
        {
            // Tampilkan ulang pertanyaan yang sama
            ShowQuestion();
        }
    }

    public void CloseQuiz()
    {
        ResetButtonScales();
        SetFeedbackOutline(false, false);
        isQuizActive = false;
        canPressEToClose = false;
        isShowingFeedback = false;
        if (choicesPanel != null) choicesPanel.SetActive(false);
        if (questionText != null) questionText.text = string.Empty;
    }

    private void EndQuiz()
    {
        StopAllCoroutines();
        ResetButtonScales();
        SetFeedbackOutline(false, false);
        isQuizActive = false;
        canPressEToClose = false;
        isShowingFeedback = false;
        if (choicesPanel != null) choicesPanel.SetActive(false);
        if (questionText != null) questionText.text = string.Empty;

        System.Action completed = onQuizCompleted;
        onQuizCompleted = null;
        completed?.Invoke();
    }

    private void SetupOptionButtonHovers()
    {
        if (!enableButtonHover || optionButtons == null) return;

        foreach (var btn in optionButtons)
        {
            if (btn == null) continue;

            TriviaButtonHover hoverComp = btn.GetComponent<TriviaButtonHover>();
            if (hoverComp == null)
            {
                hoverComp = btn.gameObject.AddComponent<TriviaButtonHover>();
            }

            hoverComp.Initialize(hoverScale, hoverTransitionSpeed, buttonHoverSFX, audioSource, sfxVolume);
        }
    }

    private void ResetButtonScales()
    {
        if (optionButtons == null) return;
        foreach (var btn in optionButtons)
        {
            if (btn == null) continue;
            var hoverComp = btn.GetComponent<TriviaButtonHover>();
            if (hoverComp != null) hoverComp.ResetScaleInstant();
        }
    }

    private void SetupFeedbackOutline()
    {
        if (questionText == null || questionText.transform.parent == null) return;

        Graphic borderGraphic = questionText.transform.parent.GetComponent<Graphic>();
        if (borderGraphic == null) return;

        feedbackOutline = borderGraphic.GetComponent<Outline>();
        if (feedbackOutline == null)
        {
            feedbackOutline = borderGraphic.gameObject.AddComponent<Outline>();
        }

        feedbackOutline.effectDistance = new Vector2(6f, 6f);
        feedbackOutline.useGraphicAlpha = false;
        feedbackOutline.enabled = false;
    }

    private void SetFeedbackOutline(bool visible, bool isCorrect)
    {
        if (feedbackOutline == null) return;

        feedbackOutline.effectColor = isCorrect ? correctBorderColor : wrongBorderColor;
        feedbackOutline.enabled = visible;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip, sfxVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, sfxVolume);
        }
    }
}
