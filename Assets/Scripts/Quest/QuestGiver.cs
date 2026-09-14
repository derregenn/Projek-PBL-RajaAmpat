using UnityEngine;

public class QuestGiver : MonoBehaviour
{
    [SerializeField] private Quest questToGive;

    public void GiveQuest()
    {
        if (questToGive == null)
        {
            Debug.LogWarning(
                "Quest Giver tidak memiliki quest."
            );
            return;
        }

        if (QuestController.Instance == null)
        {
            Debug.LogError(
                "QuestController tidak ditemukan."
            );
            return;
        }

        if (QuestController.Instance.IsQuestActive(
            questToGive.QuestID))
        {
            Debug.Log("Quest already active.");
            return;
        }

        QuestController.Instance.AcceptQuest(
            questToGive
        );

        Debug.Log(
        "QUEST DIBERIKAN: " + questToGive.QuestTitle
        );
    }
}