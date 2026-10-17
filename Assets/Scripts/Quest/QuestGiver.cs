using UnityEngine;

public class QuestGiver : MonoBehaviour
{
    [Header("Quest")]
    [SerializeField] private Quest questToGive;

    [Header("Physical Collision (2D)")]
    [SerializeField] private bool giveQuestOnPlayerCollision = true;
    [SerializeField] private bool requireBoxCollider2D;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!giveQuestOnPlayerCollision || collision == null)
        {
            return;
        }

        if (!collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        if (requireBoxCollider2D && !(collision.collider is BoxCollider2D))
        {
            return;
        }

        GiveQuest();
    }

    public void GiveQuest()
    {
        if (questToGive == null)
        {
            Debug.LogWarning(
                "QuestGiver: Quest To Give belum diisi."
            );
            return;
        }

        if (QuestController.Instance == null)
        {
            Debug.LogError(
                "QuestGiver: QuestController tidak ditemukan."
            );
            return;
        }

        if (QuestController.Instance.IsQuestActive(
            questToGive.QuestID))
        {
            Debug.Log(
                "Quest sudah aktif: " +
                questToGive.QuestID
            );
            return;
        }

        QuestController.Instance.AcceptQuest(
            questToGive
        );

        Debug.Log(
            "NPC memberikan quest: " +
            questToGive.QuestTitle
        );
    }
}