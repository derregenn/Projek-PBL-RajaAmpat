using UnityEngine;
using UnityEngine.Events;

public class QuestObjectiveTrigger : MonoBehaviour
{
    [Header("Objective")]
    [SerializeField] private string objectiveID;

    [Header("Events")]
    [SerializeField] private UnityEvent onObjectiveTriggered;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (hasTriggered)
            return;

        if (QuestController.Instance == null)
        {
            Debug.LogWarning(
                "QuestObjectiveTrigger: QuestController tidak ditemukan."
            );
            return;
        }

        if (!HasActiveObjective())
        {
            Debug.Log(
                "Objective belum aktif: " +
                objectiveID
            );
            return;
        }

        hasTriggered = true;

        Debug.Log(
            "Objective Triggered: " +
            objectiveID
        );

        onObjectiveTriggered?.Invoke();
    }

    private bool HasActiveObjective()
    {
        foreach (Quest quest in QuestController.Instance.ActiveQuests)
        {
            if (quest == null)
                continue;

            QuestObjective objective =
                quest.GetObjective(objectiveID);

            if (objective != null &&
                !objective.IsComplete)
            {
                return true;
            }
        }

        return false;
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}