using UnityEngine;

public class QuestGiver : MonoBehaviour
{
    [Header("Quest")]
    [SerializeField] private Quest questToGive;

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