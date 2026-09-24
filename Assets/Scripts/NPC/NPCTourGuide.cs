using UnityEngine;

public class NPCTourGuide : MonoBehaviour
{
    [Header("Tour Guide Settings")]
    [SerializeField] private string tourGuideName = "Tour Guide";
    [SerializeField] private string objectiveID = "finish_trivia_quiz";

    [Header("Questions Setup")]
    [SerializeField] private TriviaQuestion[] quizQuestions;

    [Header("References")]
    [SerializeField] private Dialogue dialogueSystem; // Hubungkan ke skrip Dialogue NPC ini

    private bool isQuizFinished = false;

    private void Awake()
    {
        // Otomatis mengambil komponen Dialogue jika berada di GameObject yang sama
        if (dialogueSystem == null)
        {
            dialogueSystem = GetComponent<Dialogue>();
        }
    }

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
            // Mulai kuis lewat TriviaQuizUI
            TriviaQuizUI.Instance.StartQuiz(quizQuestions, OnQuizSuccess);
        }
        else
        {
            Debug.LogWarning("TriviaQuizUI.Instance belum ada di Scene atau Quiz Questions masih kosong!");
        }
    }

    private void OnQuizSuccess()
    {
        isQuizFinished = true;
        Debug.Log("Kuis berhasil diselesaikan!");

        // Update progres quest
        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, 1);
        }
    }
}