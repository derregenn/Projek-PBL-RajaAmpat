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

    [Header("Close Settings")]
    [SerializeField] private KeyCode closeKey = KeyCode.E;
    [SerializeField] private Button closeButton; // Opsional: Hubungkan ke Button UI 'X' atau 'E' jika ada

    private TriviaQuestion[] currentQuestions;
    private int currentQuestionIndex = 0;
    private System.Action onQuizCompleted;
    private bool isQuizActive = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (quizPanel != null) quizPanel.SetActive(false);
    }

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseQuiz);
        }
    }

    private void Update()
    {
        // Jika kuis sedang aktif dan player menekan tombol E
        if (isQuizActive && Input.GetKeyDown(closeKey))
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

        if (quizPanel != null) quizPanel.SetActive(true);
        if (feedbackText != null) feedbackText.text = "";

        ShowQuestion();
    }

    private void ShowQuestion()
    {
        if (currentQuestionIndex >= currentQuestions.Length)
        {
            EndQuiz();
            return;
        }

        TriviaQuestion q = currentQuestions[currentQuestionIndex];
        questionTextUI.text = q.questionText;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < q.options.Length)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionTexts[i].text = q.options[i];

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
        // A delayed click can arrive after the last answer increments the index.
        if (!isQuizActive || currentQuestions == null ||
            currentQuestionIndex < 0 || currentQuestionIndex >= currentQuestions.Length)
        {
            return;
        }

        TriviaQuestion q = currentQuestions[currentQuestionIndex];

        if (q == null || q.options == null || selectedIndex < 0 ||
            selectedIndex >= q.options.Length)
        {
            return;
        }

        if (selectedIndex == q.correctAnswerIndex)
        {
            CancelInvoke(nameof(ClearFeedback));
            if (feedbackText != null) feedbackText.text = "<color=green>Jawaban Benar!</color>";
            currentQuestionIndex++;
            Invoke(nameof(NextQuestion), 1f);
        }
        else
        {
            if (feedbackText != null) feedbackText.text = "<color=red>Jawaban Salah, coba lagi!</color>";
            CancelInvoke(nameof(ClearFeedback));
            Invoke(nameof(ClearFeedback), 2f);
        }
    }

    private void ClearFeedback()
    {
        if (feedbackText != null) feedbackText.text = "";
    }

    private void NextQuestion()
    {
        if (feedbackText != null) feedbackText.text = "";
        ShowQuestion();
    }

    public void CloseQuiz()
    {
        isQuizActive = false;
        CancelInvoke(nameof(ClearFeedback));
        if (quizPanel != null) quizPanel.SetActive(false);
        Debug.Log("Kuis ditutup oleh pemain.");
    }

    private void EndQuiz()
    {
        isQuizActive = false;
        if (quizPanel != null) quizPanel.SetActive(false);
        Debug.Log("Kuis Selesai!");

        onQuizCompleted?.Invoke();
    }
}