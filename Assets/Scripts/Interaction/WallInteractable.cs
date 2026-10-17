using UnityEngine;

public class WallInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt Settings")]
    [SerializeField] private string promptMessage = "Panjat Tebing";
    [SerializeField] private bool climbDown;

    public void Interact()
    {
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
        return promptMessage;
    }
}