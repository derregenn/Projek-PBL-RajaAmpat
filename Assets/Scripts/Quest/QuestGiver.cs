using UnityEngine;

public class QuestGiver : MonoBehaviour
{
    [Header("Quest")]
    [SerializeField] private Quest[] questsToGive = new Quest[0];

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
        Quest quest = GetFirstAvailableQuest();

        if (quest == null)
        {
            Debug.LogWarning(
                "QuestGiver: Belum ada quest yang diisi."
            );
            return;
        }

        GiveQuest(quest);
    }

    public void GiveQuest(int index)
    {
        Quest quest = GetQuestAt(index);

        if (quest == null)
        {
            Debug.LogWarning(
                "QuestGiver: Quest index " + index + " tidak valid."
            );
            return;
        }

        GiveQuest(quest);
    }

    public void GiveQuest(Quest quest)
    {
        if (quest == null)
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
            quest.QuestID))
        {
            Debug.Log(
                "Quest sudah aktif: " +
                quest.QuestID
            );
            return;
        }

        QuestController.Instance.AcceptQuest(
            quest
        );

        Debug.Log(
            "NPC memberikan quest: " +
            quest.QuestTitle
        );
    }

    private Quest GetFirstAvailableQuest()
    {
        if (questsToGive == null || questsToGive.Length == 0)
        {
            return null;
        }

        foreach (Quest quest in questsToGive)
        {
            if (quest != null)
            {
                return quest;
            }
        }

        return null;
    }

    private Quest GetQuestAt(int index)
    {
        if (questsToGive == null || index < 0 || index >= questsToGive.Length)
        {
            return null;
        }

        return questsToGive[index];
    }
}