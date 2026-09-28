using UnityEngine;

public class BarrierInteractable : MonoBehaviour, IInteractable
{
    [Header("Messages")]
    [SerializeField] private string lockedPromptText = "";

    public void Interact()
    {
        // Opsional: Buka pop-up dialog/trivia atau sampaikan info singkat saat ditekan 'E'
        Debug.Log("Jalan masih terkunci oleh barrier.");
    }

    public string GetPromptText()
    {
        return lockedPromptText;
    }
}