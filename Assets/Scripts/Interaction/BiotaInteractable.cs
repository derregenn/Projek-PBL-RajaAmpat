using UnityEngine;

public class BiotaInteractable : MonoBehaviour, IInteractable
{
    [Header("Behavior")]
    [SerializeField] private string biotaName = "Penyu Laut";
    [SerializeField] private SequenceQTE qteToTrigger; // Drag GameObject QTE3 di Inspector
    [SerializeField] private string objectiveID = "inspect_turtle";

    private bool isDone = false;

    public void Interact()
    {
        if (isDone) return;

        // 1. Jika ada QTE yang dipasang, jalankan QTE
        if (qteToTrigger != null)
        {
            qteToTrigger.StartQTE();

            // Tambahkan Listener agar ketika QTE Sukses, otomatis selesaikan Quest
            qteToTrigger.onSuccess.RemoveAllListeners();
            qteToTrigger.onSuccess.AddListener(OnQTESuccess);
            return;
        }

        // 2. Jika tidak ada QTE, langsung selesaikan interaksi
        OnQTESuccess();
    }

    private void OnQTESuccess()
    {
        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, 1);
        }

        isDone = true;
        Debug.Log($"Interaksi dengan {biotaName} selesai!");
    }

    public string GetPromptText() => isDone ? biotaName : $"Bantu {biotaName}";
}