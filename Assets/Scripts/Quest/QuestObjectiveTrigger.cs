using UnityEngine;

public class QuestObjectiveTrigger : MonoBehaviour
{
    [SerializeField] private string objectiveID;

    public void CompleteObjective()
    {
        if (QuestController.Instance == null)
        {
            Debug.LogError(
                "QuestController tidak ditemukan."
            );
            return;
        }

        QuestController.Instance.ProgressObjective(
            objectiveID
        );
    }
}