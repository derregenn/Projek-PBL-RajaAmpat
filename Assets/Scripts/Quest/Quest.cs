using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New Quest",
    menuName = "Quest/New Quest"
)]
public class Quest : ScriptableObject
{
    // Reset progress whenever the quest asset is loaded, so a new game starts from zero.
    private void OnEnable()
    {
        ResetProgress();
    }

    [Header("Quest Information")]
    public string QuestID;
    public string QuestTitle;

    [TextArea]
    public string Description;

    [Header("Objectives")]
    public List<QuestObjective> Objectives = new List<QuestObjective>();

    // Menghitung total progres terkumpul dari semua objective quest ini
    public int CurrentAmount
    {
        get
        {
            if (Objectives == null || Objectives.Count == 0) return 0;

            int total = 0;
            foreach (var objective in Objectives)
            {
                if (objective != null)
                {
                    total += objective.CurrentAmount;
                }
            }
            return total;
        }
    }

    // Menghitung total target yang dibutuhkan dari semua objective quest ini
    public int RequiredAmount
    {
        get
        {
            if (Objectives == null || Objectives.Count == 0) return 0;

            int total = 0;
            foreach (var objective in Objectives)
            {
                if (objective != null)
                {
                    total += objective.RequiredAmount;
                }
            }
            return total;
        }
    }

    public bool IsComplete
    {
        get
        {
            if (Objectives == null || Objectives.Count == 0)
                return false;

            foreach (QuestObjective objective in Objectives)
            {
                if (objective == null || !objective.IsComplete)
                    return false;
            }

            return true;
        }
    }

    public QuestObjective GetObjective(string objectiveID)
    {
        return Objectives.Find(o => o.ObjectiveID == objectiveID);
    }

    // Fungsi reset otomatis untuk mengembalikan progres semua objective ke 0
    public void ResetProgress()
    {
        if (Objectives == null) return;

        foreach (var objective in Objectives)
        {
            if (objective != null)
            {
                objective.CurrentAmount = 0;
            }
        }
    }
}