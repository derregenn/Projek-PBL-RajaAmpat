using UnityEngine;
public class MisoolUnderwaterMonologue : MonoBehaviour
{
    public void OnMonologueComplete()
    {
        if (PlayerPrefs.GetInt("Misool_QuestStage", 0) == 2)
        {
            PlayerPrefs.SetInt("Misool_QuestStage", 3); PlayerPrefs.Save();
        }
    }
}