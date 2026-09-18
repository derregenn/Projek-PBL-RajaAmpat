using UnityEngine;

public class BiotaInteractable : MonoBehaviour, IInteractable
{
    [Header("Behavior")]
    [SerializeField] private string biotaName = "Penyu Laut";
    [SerializeField] private QTE2 qteToTrigger; // Drag objek QTE2 jika butuh hold E
    [SerializeField] private string objectiveID = "inspect_turtle";

    private bool isDone = false;

    public void Interact()
    {
        if (isDone) return;

        // Jika biota butuh mini-game hold QTE
        if (qteToTrigger != null)
        {
            qteToTrigger.ResetQTE();
            return;
        }

        // Jika hanya interaksi langsung
        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, 1);
        }

        isDone = true;
    }

    public string GetPromptText() => isDone ? biotaName : $"Dekati {biotaName}";
}