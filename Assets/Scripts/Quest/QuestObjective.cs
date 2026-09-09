using System;
using UnityEngine;

public enum QuestObjectiveType
{
    Interact,
    Talk,
    Trivia,
    Hold,
    Photo,
    QTE,
    Collect,
    Fetch,
    Deliver,
    Diving
}

[Serializable]
public class QuestObjective
{
    public string ObjectiveID;

    public QuestObjectiveType Type;

    [TextArea]
    public string Description;

    public int RequiredAmount = 1;

    [HideInInspector]
    public int CurrentAmount = 0;

    public bool IsComplete
    {
        get
        {
            return CurrentAmount >= RequiredAmount;
        }
    }

    public void AddProgress(int amount = 1)
    {
        CurrentAmount += amount;

        if (CurrentAmount > RequiredAmount)
        {
            CurrentAmount = RequiredAmount;
        }
    }
}