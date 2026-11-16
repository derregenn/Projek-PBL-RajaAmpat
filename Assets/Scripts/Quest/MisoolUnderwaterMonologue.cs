using UnityEngine;

public class MisoolUnderwaterMonologue : MonoBehaviour
{
    public void OnMonologueComplete()
    {
        int currentStage = MasterQuestManager.Instance != null 
            ? MasterQuestManager.Instance.CurrentStage 
            : PlayerPrefs.GetInt("Misool_QuestStage", 0);

        if (currentStage == 2)
        {
            if (MasterQuestManager.Instance != null)
            {
                MasterQuestManager.Instance.AdvanceStageTo(3);
            }
            else
            {
                PlayerPrefs.SetInt("Misool_QuestStage", 3);
                PlayerPrefs.Save();
            }
            Debug.Log("Misool underwater monologue completed. Quest stage advanced to 3.");
        }
        else
        {
            Debug.Log($"Misool underwater monologue completed, but quest stage was {currentStage} (expected 2). No progression applied.");
        }
    }
}
