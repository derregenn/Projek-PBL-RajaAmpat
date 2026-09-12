using UnityEngine;

public class TrashInteractable : MonoBehaviour, IInteractable
{
    [Header("Quest Integration")]
    [SerializeField] private string objectiveID = "collect_trash";
    [SerializeField] private int amountToAdd = 1;

    [Header("Audio & FX (Opsional)")]
    [SerializeField] private AudioClip collectSound;

    public void Interact()
    {
        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, amountToAdd);
        }

        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        Destroy(gameObject);
    }

    public string GetPromptText() => "Ambil Sampah";
}