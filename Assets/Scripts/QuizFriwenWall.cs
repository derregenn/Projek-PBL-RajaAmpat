using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class QuizFriwenWall : MonoBehaviour
{
    [Header("UI Panel & Options")]
    public GameObject quizPanel;
    public Button buttonOptionA; // Benar (Makhluk hidup rapuh)
    public Button buttonOptionB; // Salah (Dinding keras)
    public Button buttonOptionC; // Salah (Ikan marah)

    [Header("Quest Objective")]
    [SerializeField] private string objectiveID = "foto_pasir_timbul";

    [Header("Events")]
    public UnityEvent onAnswerCorrect;
    public UnityEvent onAnswerWrong;

    private void Start()
    {
        if (quizPanel != null) quizPanel.SetActive(false);

        if (buttonOptionA != null) buttonOptionA.onClick.AddListener(SelectCorrectAnswer);
        if (buttonOptionB != null) buttonOptionB.onClick.AddListener(SelectWrongAnswer);
        if (buttonOptionC != null) buttonOptionC.onClick.AddListener(SelectWrongAnswer);
    }

    public void ShowQuiz()
    {
        if (quizPanel != null) quizPanel.SetActive(true);
    }

    public void SelectCorrectAnswer()
    {
        if (quizPanel != null) quizPanel.SetActive(false);

        // Update Quest ke Pasir Timbul
        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, 1);
        }

        onAnswerCorrect?.Invoke();
    }

    public void SelectWrongAnswer()
    {
        if (quizPanel != null) quizPanel.SetActive(false);

        onAnswerWrong?.Invoke();
    }
}