using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TriviaQuizUI : MonoBehaviour
{
    public static TriviaQuizUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Dialogue dialogueSystem;     // Drag NPC_Warlok2 (Dialogue) ke sini
    [SerializeField] private GameObject choicesPanel;      // Panel penampung 3 tombol
    [SerializeField] private Button[] optionButtons;       // 3 Tombol Jawaban
    [SerializeField] private TextMeshProUGUI[] optionTexts;// Teks pada 3 tombol

    [Header("Feedback per Question")]
    [SerializeField] private string[] correctFeedbackMessages;
    [SerializeField] private string[] wrongFeedbackMessages;

    private TriviaQuestion[] currentQuestions;
    private int currentQuestionIndex = 0;
    private System.Action onQuizCompleted;
    private bool isQuizActive = false;
    private bool isShowingFeedback = false;

    private void Awake()
    {
        Instance = this;
        if (choicesPanel != null) choicesPanel.SetActive(false);
    }

    public void StartQuiz(TriviaQuestion[] questions, System.Action onComplete)
    {
        currentQuestions = questions;
        currentQuestionIndex = 0;
        onQuizCompleted = onComplete;
        isQuizActive = true;
        isShowingFeedback = false;

        ShowQuestion();
    }

    private void ShowQuestion()
    {
        if (currentQuestions == null || currentQuestionIndex >= currentQuestions.Length)
        {
            EndQuiz();
            return;
        }

        TriviaQuestion q = currentQuestions[currentQuestionIndex];

        if (dialogueSystem != null)
        {
            dialogueSystem.ShowTriviaQuestion(q.questionText);
        }

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

        TriviaQuestion q = currentQuestions[currentQuestionIndex];

        if (selectedIndex == q.correctAnswerIndex)
        {
            string successMsg = GetFeedbackMessage(correctFeedbackMessages, currentQuestionIndex, "Naise, jawaban kamu benar!");
            StartCoroutine(ShowFeedbackRoutine(successMsg, true));
        }
        else
        {
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

        if (choicesPanel != null) choicesPanel.SetActive(false);

        if (dialogueSystem != null)
        {
            dialogueSystem.ShowTriviaQuestion(feedbackMessage);
        }

        yield return new WaitForSeconds(1.5f);

        isShowingFeedback = false;

        if (isCorrect)
        {
            currentQuestionIndex++;
            ShowQuestion();
        }
        else
        {
            ShowQuestion();
        }
    }

    public void CloseQuiz()
    {
        isQuizActive = false;
        isShowingFeedback = false;
        if (choicesPanel != null) choicesPanel.SetActive(false);
    }

    private void EndQuiz()
    {
        isQuizActive = false;
        isShowingFeedback = false;

        // Cukup matikan 3 tombol pilihan kuis saja!
        if (choicesPanel != null) choicesPanel.SetActive(false);

        // Panggil event selesai kuis (akan memicu Dialog Post-Trivia di NPCTourGuide)
        onQuizCompleted?.Invoke();
    }
}