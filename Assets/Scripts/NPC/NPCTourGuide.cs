using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class NPCTourGuide : MonoBehaviour
{
    [Header("Tour Guide Settings")]
    [SerializeField] private string tourGuideName = "Tour Guide";
    [SerializeField] private string objectiveID = "finish_trivia_quiz";

    [Header("Questions Setup")]
    [SerializeField] private TriviaQuestion[] quizQuestions;

    [Header("References")]
    [SerializeField] private Dialogue dialogueSystem;       // Script Dialogue Awal Warlok 2
    [SerializeField] private Dialogue postTriviaDialogue;   // Script Dialogue Post-Trivia (Canvas_PostTrivia)
    [SerializeField] private QuestGiver questGiver;         // Script QuestGiver (Quest Foto)

    [Header("Events")]
    public UnityEvent onQuizSuccessEvent; 
    public UnityEvent onDialogueEndEvent;

    private bool isQuizFinished = false;

    private void Awake()
    {
        if (dialogueSystem == null) dialogueSystem = GetComponent<Dialogue>();
        if (questGiver == null) questGiver = GetComponent<QuestGiver>();
    }

    /// <summary>
    /// Dipanggil oleh On Dialogue End milik Dialogue Awal Warlok 2
    /// </summary>
    public void StartQuizAfterDialogue()
    {
        if (isQuizFinished) return;
        StartCoroutine(RoutineStartQuiz());
    }

    private IEnumerator RoutineStartQuiz()
    {
        // Jeda 1 frame agar pencetan tombol [E] dari dialog tidak mengganggu kuis
        yield return null;

        if (TriviaQuizUI.Instance != null && quizQuestions != null && quizQuestions.Length > 0)
        {
            TriviaQuizUI.Instance.StartQuiz(quizQuestions, OnQuizSuccess);
        }
        else
        {
            OnQuizSuccess();
        }
    }

    public void Interact()
    {
        if (isQuizFinished) return;
        
        if (dialogueSystem != null)
        {
            dialogueSystem.StartDialogue();
        }
    }

    private void OnQuizSuccess()
    {
        isQuizFinished = true;
        Debug.Log("[Friwen] Kuis Trivia Berhasil!");

        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, 1);
        }

        onQuizSuccessEvent?.Invoke();
        onDialogueEndEvent?.Invoke();

        if (postTriviaDialogue != null)
        {
            postTriviaDialogue.gameObject.SetActive(true);
            postTriviaDialogue.StartDialogue();
        }
        else if (questGiver != null)
        {
            questGiver.GiveQuest();
        }
    }

    /// <summary>
    /// Dipanggil saat dialog Post-Trivia selesai
    /// </summary>
    public void OnPostTriviaDialogueEnd()
    {
        if (questGiver != null)
        {
            questGiver.GiveQuest();
            Debug.Log("[Friwen] Quest Foto Pasir Timbul Diberikan!");
        }
    }
}