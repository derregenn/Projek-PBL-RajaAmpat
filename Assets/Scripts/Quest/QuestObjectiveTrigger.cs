using UnityEngine;
using UnityEngine.Events;

public class QuestObjectiveTrigger : MonoBehaviour
{
    [Header("Objective Settings")]
    public string objectiveID = "press_interact";
    public int amountToAdd = 1;

    [Header("Trigger Mode")]
    [Tooltip("Centang jika quest langsung selesai saat disentuh. Hilangkan centang jika quest diselesaikan oleh QTE.")]
    public bool progressObjectiveOnTrigger = false;

    [Header("Events")]
    public UnityEvent OnObjectiveTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Deteksi player fleksibel (termasuk jika collider ada di child object player)
        bool isPlayer = other.CompareTag("Player") || 
                        (other.transform.root != null && other.transform.root.CompareTag("Player")) ||
                        other.name.ToLower().Contains("player");

        if (!isPlayer) return;

        Debug.Log($"[Trigger Log] Player BERHASIL menyentuh area '{gameObject.name}'!");

        if (QuestController.Instance == null)
        {
            Debug.LogError("[Trigger Log] ERROR: QuestController.Instance TIDAK DITEMUKAN di scene!");
            return;
        }

        // Cek apakah objective quest sedang aktif dan belum selesai
        bool isObjectiveActive = false;
        foreach (var quest in QuestController.Instance.ActiveQuests)
        {
            if (quest == null) continue;
            var obj = quest.GetObjective(objectiveID);
            if (obj != null && !obj.IsComplete)
            {
                isObjectiveActive = true;
                break;
            }
        }

        if (isObjectiveActive)
        {
            Debug.Log($"[Trigger Log] SUCCESS: Objective '{objectiveID}' AKTIF! Memanggil UI QTE...");
            
            if (progressObjectiveOnTrigger)
            {
                QuestController.Instance.ProgressObjective(objectiveID, amountToAdd);
            }

            OnObjectiveTriggered?.Invoke();
        }
        else
        {
            Debug.LogWarning($"[Trigger Log] WARNING: Player menyentuh pohon, tetapi Objective ID '{objectiveID}' TIDAK ADA di daftar quest aktif saat ini!");
        }
    }
}