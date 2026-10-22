using UnityEngine;
using UnityEngine.Events; // Wajib untuk UnityEvent

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
    public UnityEvent onQuizSuccessEvent; 
    public UnityEvent onDialogueEndEvent; // 👈 Ditambahkan agar slot "On Dialogue End" muncul di Inspector

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
            Debug.Log($"{tourGuideName}: Kamu sudah menyelesaikan kuis ini!");
            return;
        }

        if (TriviaQuizUI.Instance != null && quizQuestions.Length > 0)
        {
            TriviaQuizUI.Instance.StartQuiz(quizQuestions, OnQuizSuccess);
        }
        else
        {
            // Jika tidak ada kuis, langsung jalankan event selesai
            OnQuizSuccess();
        }
    }

    private void OnQuizSuccess()
    {
        isQuizFinished = true;
        Debug.Log("Kuis/Dialog berhasil diselesaikan!");

        // Update progres quest
        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, 1);
        }

        // Panggil event Inspector jika ada
        onQuizSuccessEvent?.Invoke();
        onDialogueEndEvent?.Invoke();

        // 🚀 AUTO-LOAD: Memanggil pindah scene ke Pianemo secara otomatis lewat kode
        CutsceneSceneLoader loader = FindObjectOfType<CutsceneSceneLoader>();
        if (loader != null)
        {
            loader.FinishCutsceneAndLoadIsland();
        }
    }
}