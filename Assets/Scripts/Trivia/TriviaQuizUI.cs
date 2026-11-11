using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TriviaQuizUI : MonoBehaviour
{
    public static TriviaQuizUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject choicesPanel;      // Panel penampung 3 tombol di bawah
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private Button[] optionButtons;       // 3 Tombol Jawaban
    [SerializeField] private TextMeshProUGUI[] optionTexts;// Teks pada 3 tombol

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

    private void Awake()
    {
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

        // Setup audio source otomatis jika belum diset di inspector
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        SetupOptionButtonHovers();
    }

    private void Update()
    {
        if (isQuizActive && canPressEToClose && Input.GetKeyDown(closeKey) && !isShowingFeedback)
        {
            CloseQuiz();
        }
    }

    public void StartQuiz(TriviaQuestion[] questions, System.Action onComplete)
    {
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
        if (currentQuestionIndex >= currentQuestions.Length)
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

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < q.options.Length)
            {
                optionButtons[i].gameObject.SetActive(true);
                if (i < optionTexts.Length && optionTexts[i] != null)
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

            // Jawaban benar menampilkan feedback sebelum lanjut ke soal berikutnya.
            string successMsg = GetFeedbackMessage(correctFeedbackMessages, currentQuestionIndex, "Naise, jawaban kamu benar!");
            StartCoroutine(ShowFeedbackRoutine(successMsg, true));
        }
        else
        {
            // SFX Jawaban Salah
            PlaySFX(wrongAnswerSFX);

            // Jawaban salah menampilkan feedback sebelum mengulang soal.
            string wrongMsg = GetFeedbackMessage(wrongFeedbackMessages, currentQuestionIndex, "Salah, coba lagi ya!");
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
