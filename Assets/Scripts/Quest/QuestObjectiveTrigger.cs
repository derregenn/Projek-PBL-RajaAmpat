using UnityEngine;
using UnityEngine.Events;

public class QuestObjectiveTrigger : MonoBehaviour
{
    [Header("Objective Settings")]
    [Tooltip("ID Objective pada ScriptableObject Quest yang ingin dipicu.")]
    public string objectiveID = "press_interact";

    [Tooltip("Jumlah penambahan progress saat trigger terpicu.")]
    public int amountToAdd = 1;

    [Header("Trigger Conditions")]
    [Tooltip("Jika dicentang, trigger HANYA akan bereaksi jika objective ini sedang aktif di quest pemain.")]
    [SerializeField] private bool requireObjectiveActive = true;

    [Tooltip("Jika dicentang, trigger hanya bisa terpicu sekali lalu non-aktif.")]
    [SerializeField] private bool triggerOnce = true;

    [Header("Progress Mode")]
    [Tooltip("Centang jika trigger ini langsung menambah progres/menyelesaikan objective saat disentuh. " +
             "Hilangkan centang jika progres quest diselesaikan oleh mekanisme lain seperti QTE/interaksi tombol.")]
    public bool progressObjectiveOnTrigger = false;

    [Header("Events")]
    [Tooltip("Dipanggil saat trigger berhasil disentuh oleh player (misal untuk memunculkan QTE, dialog, atau efek suara).")]
    public UnityEvent OnObjectiveTriggered;

    [Tooltip("HANYA dipanggil jika objective ini telah BENAR-BENAR SELESAI (CurrentAmount >= RequiredAmount). " +
             "Gunakan event ini jika ingin memicu dialog kelanjutan atau quest berikutnya agar tidak menimpa quest sebelum tuntas!")]
    public UnityEvent OnObjectiveCompleted;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered && triggerOnce) return;

        // Deteksi player fleksibel (termasuk jika collider ada di child object player)
        bool isPlayer = other.CompareTag("Player") ||
                        (other.transform.root != null && other.transform.root.CompareTag("Player")) ||
                        other.name.ToLower().Contains("player");

        if (!isPlayer) return;

        // Cari apakah objective ini aktif di quest yang sedang berjalan
        Quest targetQuest = null;
        QuestObjective targetObj = null;

        if (QuestController.Instance != null)
        {
            foreach (var q in QuestController.Instance.ActiveQuests)
            {
                if (q == null) continue;
                var obj = q.GetObjective(objectiveID);
                if (obj != null && !obj.IsComplete)
                {
                    targetQuest = q;
                    targetObj = obj;
                    break;
                }
            }
        }

        bool isObjectiveActive = (targetObj != null);

        // Jika requireObjectiveActive aktif dan quest/objective belum berjalan, jangan eksekusi trigger
        if (requireObjectiveActive && !isObjectiveActive)
        {
            Debug.Log($"[QuestObjectiveTrigger] Player menyentuh '{gameObject.name}', tetapi objective '{objectiveID}' belum aktif. Trigger diabaikan.");
            return;
        }

        Debug.Log($"[QuestObjectiveTrigger] Player menyentuh area '{gameObject.name}' untuk objective '{objectiveID}'.");

        if (triggerOnce)
        {
            hasTriggered = true;
        }

        // Jalankan event sentuhan awal (misal untuk QTE)
        OnObjectiveTriggered?.Invoke();

        // Tambah progress jika mode progress on trigger aktif
        if (isObjectiveActive && progressObjectiveOnTrigger && QuestController.Instance != null)
        {
            QuestController.Instance.ProgressObjective(objectiveID, amountToAdd);

            // Cek apakah objective telah tuntas setelah ditambahkan
            if (targetObj.IsComplete)
            {
                Debug.Log($"[QuestObjectiveTrigger] Objective '{objectiveID}' telah SELESAI! Menjalankan OnObjectiveCompleted.");
                OnObjectiveCompleted?.Invoke();
            }
        }
    }

    /// <summary>
    /// Reset trigger agar bisa disentuh kembali (jika diperlukan dari script lain).
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
