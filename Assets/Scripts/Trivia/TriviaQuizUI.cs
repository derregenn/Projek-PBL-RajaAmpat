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
    [SerializeField] private TextMeshProUGUI feedbackText; // Optional: Teks "Benar/Salah"

    [Header("Close Settings")]
    [SerializeField] private KeyCode closeKey = KeyCode.E;

    private TriviaQuestion[] currentQuestions;
    private int currentQuestionIndex = 0;
    private System.Action onQuizCompleted;
    private bool isQuizActive = false;
    private bool canPressEToClose = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (choicesPanel != null) choicesPanel.SetActive(false);
    }

    private void Update()
    {
        if (isQuizActive && canPressEToClose && Input.GetKeyDown(closeKey))
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

        if (choicesPanel != null) choicesPanel.SetActive(true);
        if (feedbackText != null) feedbackText.text = "";

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

        // 1. Kirim Teks Pertanyaan ke Dialogue System bawaan
        if (dialogueSystem != null)
        {
            // Memanggil tayangan teks dialog untuk pertanyaan
            dialogueSystem.ShowTriviaQuestion(q.questionText);
        }

        // 2. Tampilkan Opsi Jawaban pada 3 Tombol Horizontal di Bawah
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
        TriviaQuestion q = currentQuestions[currentQuestionIndex];

        if (selectedIndex == q.correctAnswerIndex)
        {
            if (feedbackText != null) feedbackText.text = "<color=green>Jawaban Benar!</color>";
            StartCoroutine(ClearFeedbackAfterDelay(2f));
            currentQuestionIndex++;
            Invoke(nameof(NextQuestion), 0.8f);
        }
        else
        {
            if (feedbackText != null) feedbackText.text = "<color=red>Jawaban Salah, coba lagi!</color>";
            StartCoroutine(ClearFeedbackAfterDelay(2f));
        }
    }

    private IEnumerator ClearFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (feedbackText != null)
        {
            feedbackText.text = "";
        }
    }

    private void NextQuestion()
    {
        if (feedbackText != null) feedbackText.text = "";
        ShowQuestion();
    }

    public void CloseQuiz()
    {
        isQuizActive = false;
        canPressEToClose = false;
        if (choicesPanel != null) choicesPanel.SetActive(false);
    }

    private void EndQuiz()
    {
        isQuizActive = false;
        canPressEToClose = false;
        if (choicesPanel != null) choicesPanel.SetActive(false);

        onQuizCompleted?.Invoke();
    }
}