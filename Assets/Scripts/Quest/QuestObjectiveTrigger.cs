using UnityEngine;
using UnityEngine.Events;

public class QuestObjectiveTrigger : MonoBehaviour
{
    [Header("Objective Settings")]
    public string objectiveID = "perahu_friwen";
    public int amountToAdd = 1;

    [Header("Quest Validation")]
    [Tooltip("Jika DICENTANG, trigger HANYA berjalan kalau quest terdaftar aktif. Jika UNCHECK, trigger akan LANGSUNG memanggil QTE/Event tanpa memedulikan status quest.")]
    public bool requireActiveQuest = false;

    [Header("Trigger Mode")]
    [Tooltip("Centang jika quest langsung selesai saat disentuh. Hilangkan centang jika quest diselesaikan oleh QTE.")]
    public bool progressObjectiveOnTrigger = false;

    [Header("Events")]
    public UnityEvent OnObjectiveTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Deteksi player fleksibel
        bool isPlayer = other.CompareTag("Player") || 
                        (other.transform.root != null && other.transform.root.CompareTag("Player")) ||
                        other.name.ToLower().Contains("player");

        if (!isPlayer) return;

        Debug.Log($"[Trigger Log] Player BERHASIL menyentuh area '{gameObject.name}'!");

        // Mode Bypass: Jika tidak membutuhkan validasi quest aktif, langsung jalankan event
        if (!requireActiveQuest)
        {
            Debug.Log($"[Trigger Log] Mode Bypass Aktif! Memanggil Event/QTE untuk '{objectiveID}'...");
            ExecuteTrigger();
            return;
        }

        // Cek ketersediaan QuestController
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
            var obj = quest.GetObjective(objectiveID.Trim());
            if (obj != null && !obj.IsComplete)
            {
                isObjectiveActive = true;
                break;
            }
        }

        if (isObjectiveActive)
        {
            Debug.Log($"[Trigger Log] SUCCESS: Objective '{objectiveID}' AKTIF! Memanggil UI QTE...");
            ExecuteTrigger();
        }
        else
        {
            Debug.LogWarning($"[Trigger Log] WARNING: Player menyentuh trigger, tetapi Objective ID '{objectiveID}' TIDAK ADA di daftar quest aktif saat ini!");
        }
    }

    private void ExecuteTrigger()
    {
        if (progressObjectiveOnTrigger && QuestController.Instance != null)
        {
            QuestController.Instance.ProgressObjective(objectiveID.Trim(), amountToAdd);
        }

        OnObjectiveTriggered?.Invoke();
    }
}