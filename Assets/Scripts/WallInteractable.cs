using UnityEngine;

public class WallInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt Settings")]
    [SerializeField] private string promptMessage = "Panjat Tebing";

    public void Interact()
    {
        // Cari komponen StepClimbing di GameObject Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            StepClimbing stepClimbing = player.GetComponent<StepClimbing>();

            if (stepClimbing != null)
            {
                // Pemicu otomatis urutan panjat step-by-step saat tombol E ditekan
                stepClimbing.StartClimbSequence();
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