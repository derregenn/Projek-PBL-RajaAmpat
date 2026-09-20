using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TriviaQuizUI : MonoBehaviour
{
    public static TriviaQuizUI Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private GameObject quizPanel;
    [SerializeField] private TextMeshProUGUI questionTextUI;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TextMeshProUGUI[] optionTexts;
    [SerializeField] private TextMeshProUGUI feedbackText; // Teks "Benar!" / "Salah!"

    private TriviaQuestion[] currentQuestions;
    private int currentQuestionIndex = 0;
    private System.Action onQuizCompleted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (quizPanel != null) quizPanel.SetActive(false);
    }

    public void StartQuiz(TriviaQuestion[] questions, System.Action onComplete)
    {
        if (questions == null || questions.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        currentQuestions = questions;
        currentQuestionIndex = 0;
        onQuizCompleted = onComplete;

        if (quizPanel != null) quizPanel.SetActive(true);
        if (feedbackText != null) feedbackText.text = "";

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
        questionTextUI.text = q.questionText;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null && optionTexts != null && i < optionTexts.Length && optionTexts[i] != null && q.options != null && i < q.options.Length)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionTexts[i].text = q.options[i];

                int buttonIndex = i; // Cache index local
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(buttonIndex));
            }
            else if (optionButtons[i] != null)
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
            currentQuestionIndex++;
            Invoke(nameof(NextQuestion), 1f);
        }
        else
        {
            if (feedbackText != null) feedbackText.text = "<color=red>Jawaban Salah, coba lagi!</color>";
        }
    }

    private void NextQuestion()
    {
        if (feedbackText != null) feedbackText.text = "";
        ShowQuestion();
    }

    private void EndQuiz()
    {
        if (quizPanel != null) quizPanel.SetActive(false);
        Debug.Log("Kuis Selesai!");

        onQuizCompleted?.Invoke();
    }
}