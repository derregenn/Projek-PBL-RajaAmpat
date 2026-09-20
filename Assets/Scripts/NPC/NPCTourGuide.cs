using UnityEngine;

public class NPCTourGuide : MonoBehaviour, IInteractable
{
    [Header("Tour Guide Settings")]
    [SerializeField] private string tourGuideName = "Tour Guide";
    [SerializeField] private string objectiveID = "finish_trivia_quiz";

    [Header("Questions Setup")]
    [SerializeField] private TriviaQuestion[] quizQuestions;

    private bool isQuizFinished = false;

    public void Interact()
    {
        if (isQuizFinished)
        {
            Debug.Log($"{tourGuideName}: Terima kasih sudah menyelesaikan kuis!");
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

        // Integrasi ke QuestController jika ada
        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, 1);
        }
    }

    public string GetPromptText()
    {
        return isQuizFinished ? $"Bicara dengan {tourGuideName}" : $"Mulai Kuis bersama {tourGuideName}";
    }
}