using UnityEngine;

public class QuestGiver2 : MonoBehaviour
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
    [SerializeField] private bool giveQuestOnPlayerCollision = true;
    [SerializeField] private bool requireBoxCollider2D;

    [Header("Trigger (2D)")]
    [SerializeField] private bool giveQuestOnPlayerTrigger = true;

    private bool hasGivenQuest = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!giveQuestOnPlayerCollision || collision == null || hasGivenQuest) return;
        TryGiveQuest(collision.gameObject, collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!giveQuestOnPlayerTrigger || other == null || hasGivenQuest) return;
        TryGiveQuest(other.gameObject, other);
    }

    private void TryGiveQuest(GameObject player, Collider2D playerCollider)
    {
        bool isPlayer = player.CompareTag("Player") ||
                        (player.transform.root != null && player.transform.root.CompareTag("Player"));

        if (!isPlayer) return;

        if (requireBoxCollider2D && !(playerCollider is BoxCollider2D))
        {
            return;
        }

        GiveQuest();
    }

    public void GiveQuest()
    {
        if (questToGive == null)
        {
            Debug.LogWarning("[QuestGiver2] Quest To Give belum diisi di Inspector.");
            return;
        }

        QuestController controller = QuestController.Instance;
        if (controller == null)
        {
            controller = FindFirstObjectByType<QuestController>();
        }

        if (controller == null)
        {
            Debug.LogError("[QuestGiver2] QuestController tidak ditemukan di scene!");
            return;
        }

        if (controller.IsQuestActive(questToGive.QuestID))
        {
            Debug.Log($"[QuestGiver2] Quest '{questToGive.QuestID}' sudah aktif.");
            return;
        }

        // Cek prasyarat quest
        if (!string.IsNullOrEmpty(requiredPrerequisiteQuestID))
        {
            bool isPrereqDone = false;
            if (GamePhaseManager.Instance != null && GamePhaseManager.Instance.IsQuestCompleted(requiredPrerequisiteQuestID))
            {
                isPrereqDone = true;
            }

            if (!isPrereqDone)
            {
                Debug.LogWarning($"[QuestGiver2] Quest '{questToGive.QuestTitle}' terkunci. Selesaikan quest '{requiredPrerequisiteQuestID}' dahulu.");
                return;
            }
        }

        if (waitForCurrentQuestToFinish && controller.HasActiveQuest())
        {
            Debug.LogWarning($"[QuestGiver2] Masih ada quest aktif, quest '{questToGive.QuestTitle}' ditahan.");
            return;
        }

        hasGivenQuest = true;
        controller.AcceptQuest(questToGive);

        Debug.Log($"[QuestGiver2] NPC memberikan quest: {questToGive.QuestTitle}");
    }

    public void ResetGiver()
    {
        hasGivenQuest = false;
    }
}
