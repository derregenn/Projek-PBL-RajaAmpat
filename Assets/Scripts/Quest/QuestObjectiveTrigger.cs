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

        // QTE tetap dimulai meskipun controller/quest belum tersedia.
        // Progress objective hanya dilakukan jika objective memang aktif.
        bool isObjectiveActive = false;
        if (QuestController.Instance != null)
        {
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
        }

        if (isObjectiveActive)
        {
            if (progressObjectiveOnTrigger)
            {
                QuestController.Instance.ProgressObjective(objectiveID, amountToAdd);
            }
        }
        else
        {
            Debug.LogWarning($"[Trigger Log] Objective '{objectiveID}' tidak aktif; QTE tetap dipanggil, progress quest dilewati.");
        }

        OnObjectiveTriggered?.Invoke();
    }
}