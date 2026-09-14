using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New Quest",
    menuName = "Quest/New Quest"
)]
public class Quest : ScriptableObject
{
    [Header("Quest Information")]
    public string QuestID;
    public string QuestTitle;

    [TextArea]
    public string Description;

    [Header("Objectives")]
    public List<QuestObjective> Objectives = new List<QuestObjective>();

    public bool IsComplete
    {
        get
        {
            foreach (QuestObjective objective in Objectives)
            {
                if (!objective.IsComplete)
                    return false;
            }

            return true;
        }
    }

    public QuestObjective GetObjective(string objectiveID)
    {
        return Objectives.Find(o => o.ObjectiveID == objectiveID);
    }
}