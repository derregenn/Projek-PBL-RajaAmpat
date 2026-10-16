using UnityEngine;
using UnityEngine.Events; // Tambahkan ini wajib untuk UnityEvent

public class NPCTourGuide : MonoBehaviour
{
    [Header("Tour Guide Settings")]
    [SerializeField] private string tourGuideName = "Tour Guide";
    [SerializeField] private string objectiveID = "finish_trivia_quiz";

    [Header("Questions Setup")]
    [SerializeField] private TriviaQuestion[] quizQuestions;

    [Header("References")]
    [SerializeField] private Dialogue dialogueSystem; 

    [Header("Events")]
    // Event ini akan muncul di Inspector untuk memicu dialog lanjutan
    public UnityEvent onQuizSuccessEvent; 

    private bool isQuizFinished = false;

    private void Awake()
    {
        if (dialogueSystem == null)
        {
            dialogueSystem = GetComponent<Dialogue>();
        }
    }

    public void Interact()
    {
        if (isQuizFinished)
        {
            // Opsi: Mainkan dialog lain jika pemain ngobrol lagi setelah kuis selesai
            Debug.Log($"{tourGuideName}: Kamu sudah menyelesaikan kuis ini!");
            return;
        }

        if (TriviaQuizUI.Instance != null && quizQuestions.Length > 0)
        {
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

        // KUNCI: Panggil UnityEvent setelah kuis selesai
        if (onQuizSuccessEvent != null)
        {
            onQuizSuccessEvent.Invoke();
        }
    }
}