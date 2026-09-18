using UnityEngine;

public class WallInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt Settings")]
    [SerializeField] private string promptMessage = "Panjat Tebing";

    public void Interact()
    {
        // Fungsi ini bisa dikosongkan jika mekanisme panjat 
        // ditangani langsung oleh tombol hold E di PlayerClimbHold.cs
    }

    public string GetPromptText()
    {
        return promptMessage;
    }
}