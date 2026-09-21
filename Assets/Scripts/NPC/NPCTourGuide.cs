using UnityEngine;

public class NPCTourGuide : MonoBehaviour
{
    [Header("Tour Guide Settings")]
    [SerializeField] private string tourGuideName = "Tour Guide";
    [SerializeField] private string objectiveID = "finish_trivia_quiz";

    [Header("Questions Setup")]
    [SerializeField] private TriviaQuestion[] quizQuestions;

    private bool isQuizFinished = false;

    // Dipanggil dari Event On Dialogue End () di Inspector Dialogue.cs
    public void Interact()
    {
        if (isQuizFinished)
        {
            Debug.Log($"{tourGuideName}: Kamu sudah menyelesaikan kuis ini!");
            return;
        }

        if (TriviaQuizUI.Instance != null && quizQuestions.Length > 0)
        {
            TriviaQuizUI.Instance.StartQuiz(quizQuestions, OnQuizSuccess);
        }
    }

    private void OnQuizSuccess()
    {
        isQuizFinished = true;
        Debug.Log("Kuis berhasil diselesaikan!");

        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, 1);
        }
    }
}