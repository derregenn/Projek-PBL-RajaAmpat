using UnityEngine;
public class MisoolUnderwaterMonologue : MonoBehaviour
{
    public void OnMonologueComplete()
    {
        if (PlayerPrefs.GetInt("Misool_QuestStage", 0) == 2)
        {
            PlayerPrefs.SetInt("Misool_QuestStage", 3);
            PlayerPrefs.Save();
            Debug.Log("Misool underwater monologue completed. Quest stage advanced to 3.");
        }
        else
        {
            Debug.Log("Misool underwater monologue completed, but quest stage was not 2. No progression applied.");
        }
    }
}