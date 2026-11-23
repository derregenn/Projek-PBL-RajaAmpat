using UnityEngine;

public class WallInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt Settings")]
    [SerializeField] private string promptMessage = "Panjat Tebing";
    [SerializeField] private bool climbDown;
    public bool IsClimbDown => climbDown;

    [Header("Quest Progression Gate (Opsional)")]
    [SerializeField] private string requiredActiveObjectiveID = "";
    [SerializeField] private string requiredCompletedQuestID = "";

    public void Interact()
    {
        if (!QuestProgressionGate.Passes(requiredActiveObjectiveID, string.Empty, requiredCompletedQuestID)) return;
        // Cari komponen StepClimbing di GameObject Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            StepClimbing stepClimbing = player.GetComponent<StepClimbing>();

            if (stepClimbing != null)
            {
                if (climbDown)
                {
                    stepClimbing.StartClimbDownSequence();
                }
                else
                {
                    stepClimbing.StartClimbSequence();
                }
            }
            else
            {
                Debug.LogWarning("Skrip StepClimbing tidak ditemukan pada GameObject Player!");
            }
        }
    }

    public string GetPromptText()
    {
        if (!QuestProgressionGate.Passes(requiredActiveObjectiveID, string.Empty, requiredCompletedQuestID)) return "";
        return promptMessage;
    }
}