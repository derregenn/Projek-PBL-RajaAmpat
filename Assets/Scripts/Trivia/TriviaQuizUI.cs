using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement; // Wajib ditambahkan untuk LoadScene
using UnityEngine.Events;          // Ditambahkan untuk UnityEvent

public class TriviaQuizUI : MonoBehaviour
{
    public static TriviaQuizUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Dialogue dialogueSystem;     // Drag NPC/Dialogue ke sini
    [SerializeField] private GameObject choicesPanel;      // Panel penampung 3 tombol di bawah
    [SerializeField] private Button[] optionButtons;       // 3 Tombol Jawaban
    [SerializeField] private TextMeshProUGUI[] optionTexts;// Teks pada 3 tombol

    [Header("Close Settings")]
    [SerializeField] private KeyCode closeKey = KeyCode.E;

    [Header("Default Feedback Messages")]
    [SerializeField] private string correctFeedbackMessage = "Naise, jawaban kamu benar!";
    [SerializeField] private string wrongFeedbackMessage = "Salah, coba lagi ya!";

    [Header("Feedback per Pertanyaan")]
    [Tooltip("Pesan benar berdasarkan urutan pertanyaan di quiz. Entri kosong memakai feedback pada TriviaQuestion atau pesan default.")]
    [SerializeField] private string[] correctFeedbackByQuestion;
    [Tooltip("Pesan salah berdasarkan urutan pertanyaan di quiz. Entri kosong memakai feedback pada TriviaQuestion atau pesan default.")]
    [SerializeField] private string[] wrongFeedbackByQuestion;

    [Header("Scene Transition Settings")]
    [Tooltip("Centang jika ingin otomatis pindah scene setelah kuis selesai")]
    [SerializeField] private bool loadSceneOnQuizComplete = true;
    [Tooltip("Isi nama Scene pulau tujuan di sini (misal: 02_Waisai)")]
    [SerializeField] private string nextSceneName = "02_Waisai";
    [Tooltip("Jeda sebelum pindah scene setelah pesan feedback benar muncul")]
    [SerializeField] private float transitionDelay = 1.0f;

    [Header("Events")]
    [Tooltip("Event tambahan saat kuis selesai (misal: panggil UI Fade Out)")]
    public UnityEvent onQuizCompletedEvent;
    private TriviaQuestion[] currentQuestions;
    private int currentQuestionIndex = 0;
    private System.Action onQuizCompleted;
    private bool isQuizActive = false;
    private bool canPressEToClose = false;
    private bool isShowingFeedback = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (choicesPanel != null) choicesPanel.SetActive(false);
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
        if (currentQuestions == null || currentQuestionIndex >= currentQuestions.Length)
        {
            EndQuiz();
            return;
        }

        TriviaQuestion q = currentQuestions[currentQuestionIndex];
        if (q == null)
        {
            EndQuiz();
            return;
        }

        // 1. Tampilkan teks pertanyaan di kotak dialog NPC
        if (dialogueSystem != null)
        {
            dialogueSystem.ShowTriviaQuestion(q.questionText);
        }

        // 2. Tampilkan panel tombol opsi jawaban
        if (choicesPanel != null) choicesPanel.SetActive(true);

        // 3. Tampilkan Opsi Jawaban pada 3 Tombol Horizontal di Bawah
        if (optionButtons == null) return;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] == null) continue;

            if (q.options != null && i < q.options.Length)
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

        TriviaQuestion q = currentQuestions[currentQuestionIndex];

        if (selectedIndex == q.correctAnswerIndex)
        {
            // Jawaban Benar
            string successMsg = GetFeedbackMessage(q, true);
            StartCoroutine(ShowFeedbackRoutine(successMsg, true));
        }
        else
        {
            // Jawaban Salah
            string wrongMsg = GetFeedbackMessage(q, false);
            StartCoroutine(ShowFeedbackRoutine(wrongMsg, false));
        }
    }

    private string GetFeedbackMessage(TriviaQuestion question, bool isCorrect)
    {
        if (question == null)
        {
            return GetDefaultFeedbackMessage(isCorrect);
        }

        string[] feedbackByQuestion = isCorrect ? correctFeedbackByQuestion : wrongFeedbackByQuestion;
        if (feedbackByQuestion != null && currentQuestionIndex >= 0 &&
            currentQuestionIndex < feedbackByQuestion.Length &&
            !string.IsNullOrEmpty(feedbackByQuestion[currentQuestionIndex]))
        {
            return feedbackByQuestion[currentQuestionIndex];
        }

        string fieldName = isCorrect ? "correctFeedback" : "wrongFeedback";

        var property = typeof(TriviaQuestion).GetProperty(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (property != null)
        {
            var value = property.GetValue(question) as string;
            if (!string.IsNullOrEmpty(value)) return value;
        }

        var field = typeof(TriviaQuestion).GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (field != null)
        {
            var value = field.GetValue(question) as string;
            if (!string.IsNullOrEmpty(value)) return value;
        }

        return GetDefaultFeedbackMessage(isCorrect);
    }

    private string GetDefaultFeedbackMessage(bool isCorrect)
    {
        string message = isCorrect ? correctFeedbackMessage : wrongFeedbackMessage;
        return string.IsNullOrEmpty(message)
            ? (isCorrect ? "Naise, jawaban kamu benar!" : "Salah, coba lagi ya!")
            : message;
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
        isQuizActive = false;
        canPressEToClose = false;
        isShowingFeedback = false;
        if (choicesPanel != null) choicesPanel.SetActive(false);
    }

    private void EndQuiz()
    {
        isQuizActive = false;
        canPressEToClose = false;
        isShowingFeedback = false;
        if (choicesPanel != null) choicesPanel.SetActive(false);

        // Exec Event Callback
        onQuizCompleted?.Invoke();
        onQuizCompletedEvent?.Invoke();

        // Pindah Scene Otomatis jika diaktifkan
        if (loadSceneOnQuizComplete && !string.IsNullOrEmpty(nextSceneName))
        {
            StartCoroutine(TransitionToNextScene());
        }
    }

    private IEnumerator TransitionToNextScene()
    {
        yield return new WaitForSeconds(transitionDelay);
        SceneManager.LoadScene(nextSceneName);
    }

    // Method manual jika ingin memanggil pemindahan scene dari luar/UnityEvent
    public void LoadNextScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}