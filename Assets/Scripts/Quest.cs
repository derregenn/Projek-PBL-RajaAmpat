using UnityEngine;
using System.Collections.Generic;

public enum ObjectiveType
{
    CollectItem,
    DefeatEnemy,
    ReachLocation,
    TalkToNPC,
    Custom
}

[System.Serializable]
public class QuestObjective
{
    public string objectiveID;
    public string description;
    public ObjectiveType type;
    public int requiredAmount;
    public int currentAmount;

    public bool IsCompleted()
    {
        return currentAmount >= requiredAmount;
    }
}

[System.Serializable]
public class QuestProgress
{
    public Quest quest;
    public List<QuestObjective> objectives;

    public QuestProgress(Quest quest)
    {
        this.quest = quest;
        objectives = new List<QuestObjective>();

        if (quest != null && quest.objectives != null)
        {
            foreach (var objective in quest.objectives)
            {
                objectives.Add(new QuestObjective
                {
                    objectiveID = objective.objectiveID,
                    description = objective.description,
                    type = objective.type,
                    requiredAmount = objective.requiredAmount,
                    currentAmount = 0
                });
            }
        }
    }
}

[CreateAssetMenu(fileName = "Quest", menuName = "Scriptable Objects/Quest")]
public class Quest : ScriptableObject
{
    public string questID;
    public string questName;
    [TextArea(2, 4)] public string questDescription;
    public List<QuestObjective> objectives = new List<QuestObjective>();

    private void OnValidate()
    {
        // Otomatis membuat ID unik jika kosong
        if (string.IsNullOrEmpty(questID))
        {
            questID = System.Guid.NewGuid().ToString();
        }
    }

    public bool IsCompleted => objectives != null && objectives.Count > 0 && objectives.TrueForAll(obj => obj.IsCompleted());
    public string QuestID => questID;
}