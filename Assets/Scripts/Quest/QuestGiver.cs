using UnityEngine;

public class QuestGiver : MonoBehaviour
{
    [Header("Quest Configuration")]
    [SerializeField] private Quest questToGive;
    public Quest QuestToGive => questToGive;

    [Header("Prerequisite & Condition")]
    [Tooltip("Jika dicentang, NPC tidak akan memberikan quest baru jika masih ada quest lain yang belum diselesaikan pemain.")]
    [SerializeField] private bool waitForCurrentQuestToFinish = false;

    [Tooltip("ID Quest yang harus sudah selesai sebelum quest ini bisa diberikan (kosongkan jika tidak ada syarat).")]
    [SerializeField] private string requiredPrerequisiteQuestID = "";

    [Header("Physical Collision (2D)")]
    public bool giveQuestOnPlayerCollision = false;

    private bool hasGivenQuest = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!giveQuestOnPlayerCollision || hasGivenQuest) return;

        if (collision.CompareTag("Player") || (collision.transform.root != null && collision.transform.root.CompareTag("Player")))
        {
            GiveQuest();
        }
    }

    public void GiveQuest()
    {
        if (questToGive == null)
        {
            Debug.LogWarning("[QuestGiver] Quest To Give belum diisi di Inspector!");
            return;
        }

        QuestController controller = QuestController.Instance;
        if (controller == null)
        {
            controller = FindFirstObjectByType<QuestController>();
        }

        if (controller == null)
        {
            Debug.LogError("[QuestGiver] QuestController tidak ditemukan di scene!");
            return;
        }

        // 1. Cek apakah quest ini sendiri sudah aktif atau sudah pernah diselesaikan
        if (controller.IsQuestActive(questToGive.QuestID) ||
            QuestJournalManager.HasCompletedQuestIDPersistent(questToGive.QuestID) ||
            QuestJournalManager.IsCurrentSceneIslandCompletedPersistent())
        {
            hasGivenQuest = true;
            Debug.Log($"[QuestGiver] Quest '{questToGive.QuestID}' sudah aktif atau pulau/quest selesai, tidak diberikan lagi.");
            return;
        }

        // 2. Cek apakah ada syarat quest prasyarat yang belum tuntas
        if (!string.IsNullOrEmpty(requiredPrerequisiteQuestID))
        {
            bool isPrereqDone = false;
            if (GamePhaseManager.Instance != null && GamePhaseManager.Instance.IsQuestCompleted(requiredPrerequisiteQuestID))
            {
                isPrereqDone = true;
            }

            if (!isPrereqDone)
            {
                Debug.LogWarning($"[QuestGiver] Quest '{questToGive.QuestTitle}' belum bisa diambil. Wajib menyelesaikan quest '{requiredPrerequisiteQuestID}' terlebih dahulu.");
                return;
            }
        }

        // 3. Cek apakah ada opsi menunggu quest aktif sebelumnya selesai
        if (waitForCurrentQuestToFinish && controller.HasActiveQuest())
        {
            Debug.LogWarning($"[QuestGiver] Tidak dapat menerima '{questToGive.QuestTitle}' karena masih ada quest lain yang aktif.");
            return;
        }

        hasGivenQuest = true;
        controller.AcceptQuest(questToGive);

        Debug.Log($"[QuestGiver] Quest '{questToGive.QuestTitle}' berhasil diberikan ke pemain!");
    }

    public void ResetGiver()
    {
        hasGivenQuest = false;
    }
}
