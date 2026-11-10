using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TriviaQuizUI : MonoBehaviour
{
    public static TriviaQuizUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Dialogue dialogueSystem;     // Drag NPC/Dialogue ke sini
    [SerializeField] private GameObject choicesPanel;      // Panel penampung 3 tombol di bawah
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

        // 1. Tampilkan teks pertanyaan di kotak dialog NPC dengan format standout dua baris
        if (dialogueSystem != null)
        {
            string standoutQuestion = FormatStandoutQuestion(q.questionText, currentQuestionIndex);
            dialogueSystem.ShowTriviaQuestion(standoutQuestion);
        }

        // 2. Tampilkan panel tombol opsi jawaban (pastikan posisinya sedikit ke bawah agar tidak menutupi karakter / dialog)
        if (choicesPanel != null)
        {
            RectTransform rt = choicesPanel.GetComponent<RectTransform>();
            if (rt != null && rt.anchoredPosition.y > -20f)
            {
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -45f);
            }
            choicesPanel.SetActive(true);
        }

        // 3. Tampilkan Opsi Jawaban pada 3 Tombol Horizontal di Bawah
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

            // Jawaban Benar -> Tampilkan feedback di dialogue NPC lalu lanjut ke pertanyaan berikutnya
            string successMsg = GetFeedbackMessage(correctFeedbackMessages, currentQuestionIndex, "Naise, jawaban kamu benar!");
            StartCoroutine(ShowFeedbackRoutine(successMsg, true));
        }
        else
        {
            // SFX Jawaban Salah
            PlaySFX(wrongAnswerSFX);

            // Jawaban Salah -> Tampilkan feedback di dialogue NPC lalu ulangi pertanyaan
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

    private string FormatStandoutQuestion(string rawQuestion, int index)
    {
        if (string.IsNullOrEmpty(rawQuestion)) return "";

        string numberWord;
        switch (index)
        {
            case 0: numberWord = "pertama"; break;
            case 1: numberWord = "kedua"; break;
            case 2: numberWord = "ketiga"; break;
            case 3: numberWord = "keempat"; break;
            case 4: numberWord = "kelima"; break;
            default: numberWord = $"ke-{index + 1}"; break;
        }

        string header = $"Pertanyaan {numberWord}:";

        string body = rawQuestion.Trim();
        // Bersihkan jika pertanyaan aslinya sudah memiliki awalan "Pertanyaan pertama," dsb
        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(
            @"^Pertanyaan\s*(pertama|kedua|ketiga|keempat|kelima|terakhir|ke-\d+|\d+)\s*[,:]?\s*",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        body = regex.Replace(body, "").Trim();

        if (!string.IsNullOrEmpty(body))
        {
            body = char.ToUpper(body[0]) + body.Substring(1);
        }

        return $"<b><color=#FFD700>{header}</color></b>\n{body}";
    }

    private IEnumerator ShowFeedbackRoutine(string feedbackMessage, bool isCorrect)
    {
        isShowingFeedback = true;

        // Sembunyikan panel pilihan jawaban saat feedback ditampilkan di kotak dialog
        if (choicesPanel != null) choicesPanel.SetActive(false);

        // Kirim teks feedback langsung ke dialogue box NPC
        if (dialogueSystem != null)
        {
            dialogueSystem.ShowTriviaQuestion(feedbackMessage);
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
    }

    private void EndQuiz()
    {
        StopAllCoroutines();
        ResetButtonScales();
        isQuizActive = false;
        canPressEToClose = false;
        isShowingFeedback = false;
        if (choicesPanel != null) choicesPanel.SetActive(false);

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
